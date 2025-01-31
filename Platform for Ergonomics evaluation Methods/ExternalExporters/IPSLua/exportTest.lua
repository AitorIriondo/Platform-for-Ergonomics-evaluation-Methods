function includeThisPath()
  local filename = debug.getinfo(2, "S").source:sub(2):gsub("/","\\")
  local path = filename:sub(0, filename:match(".*".."\\".."()")-1)
  if(not package.path:find(path)) then
    package.path = package.path..';'..path..'?.lua;'
  end
end
includeThisPath()

local JSON=require("json")

local function getPosition(trans)
  --somewhat faster than get(trans,"t")
  return trans:transform(Vector3d(0,0,0))
    --local pos = get(trans,"t")
    --return pos
end

local function addVector3d(list, v)
  table.insert(list,{v.x, v.y, v.z})
end

---
--@param ips#ManikinFamily ipsFamily
local function initManikin(ipsFamily, manikinIdx)
  print(ipsFamily)
  local manikin = {
    sceneName=Ips.getSceneName(),
    operationSequenceName="",
    familyName=ipsFamily:getVisualization():getLabel(),
    familyID=ipsFamily:getID(),
    controlPoints = {},
    name=ipsFamily:getManikinNames()[manikinIdx],
    weight = ipsFamily:getMeasure(manikinIdx, "Body mass (weight)"),
    height = ipsFamily:getMeasure(manikinIdx, "Stature (body height)"),
    measures = {},
    postureTimeSteps = {},
    joints = {},
  }
  local measureNames = ipsFamily:getMeasureNames()
  for i=0, measureNames:size()-1 do
    table.insert(manikin.measures, {
      name = measureNames[i],
      value = ipsFamily:getMeasure(manikinIdx, measureNames[i])
    })
  end
  for i=0, ipsFamily:getNumJoints()-1 do
    table.insert(manikin.joints, {
      name = ipsFamily:getJointName(i),
      numAngles = ipsFamily:getNumAnglesForJoint(ipsFamily:getJointName(i)),
      positions = {},
      angles = {}
    })
  end

  manikin.update = function()
    for i, joint in ipairs(manikin.joints) do
      addVector3d(joint.positions, getPosition(ipsFamily:getJointTransformationForManikin(manikinIdx, joint.name)))
      local angV = ipsFamily:getJointAngleForManikin(manikinIdx,joint.name)
      local angles = {}
      for j=0, angV:size()-1 do
        table.insert(angles, angV[j])
      end
      table.insert(joint.angles,angles)
    end

  end
  return manikin
end

---
--@param ips#ManikinFamily ipsFamily
local function initFamily(ipsFamily)
  local family = {
    name=ipsFamily:getVisualization():getLabel(),
    id=ipsFamily:getID(),
    controlPoints = {},
    manikins = {}
  }
  for i=0, ipsFamily:getNumManikins()-1 do
    table.insert(family.manikins, initManikin(ipsFamily,i))
  end
  ---
  --@field #list<ips#ManikinControlPoint> ctrlPts
  local ctrlPts = {}
  for i=0, ipsFamily:getControlPoints():size()-1 do
    local cp = ipsFamily:getControlPoints()[i]
    table.insert(ctrlPts, cp)
    table.insert(family.controlPoints, {
      name = cp:getName(),
      id = cp:getID(),
      targets = {},
      contactForces = {},
      contactTorques = {}
    })
  end

  family.update = function()
    for i=1, #ctrlPts do
      addVector3d(family.controlPoints[i].targets, getPosition(ctrlPts[i]:getTarget()))
      addVector3d(family.controlPoints[i].contactForces, ctrlPts[i]:getResultantContactForce())
      addVector3d(family.controlPoints[i].contactTorques, ctrlPts[i]:getResultantContactTorque())
    end
    for i=1, #family.manikins do
      family.manikins[i].update()
    end
  end
  return family
end

---
--@param ips#TimelineReplay replay
local function buildTimeline(replay)
  local os=replay:getParent():toOperationSequence()
  local actors=os:getActors()
  local timeline = {
    timestamps={},
    families={}
  }
  for i=0, actors:size()-1 do
    if actors[i]:getTreeObject() then
      local famvis=actors[i]:getTreeObject():toManikinFamilyVisualization()
      if famvis then
        table.insert(timeline.families, initFamily(famvis:getManikinFamily()))
      end
    end
  end
  local dur=replay:getFinalTime()
  for t=0, dur, .03 do
    table.insert(timeline.timestamps, t)
    replay:setTime(t)
    for i=1, #timeline.families do
      timeline.families[i].update()
    end
  end
  for i, family in ipairs(timeline.families) do
    for j, manikin in ipairs(family.manikins) do
      manikin.operationSequenceName=os:getLabel()
      manikin.postureTimeSteps=timeline.timestamps
      manikin.controlPoints=family.controlPoints
    end
  end

  return timeline
end

local function writeManikinJson(manikin, dir)
  local descStr = manikin.operationSequenceName.."_"..manikin.familyName.."_"..manikin.name
  local filename=dir
  for i=1, descStr:len() do
    local char = descStr:sub(i,i)
    if not string.find("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789=-+",char) then
      char = "_"
    end
    filename = filename..char
  end
  filename = filename..".json"
  local f=io.open(filename,"w")
  f:write(JSON.encode(manikin))
  f:close()
  print("Writing ", filename)
  return filename
end

local timelineReplay = nil
if Ips.getSelection() then
  timelineReplay = Ips.getSelection():toTimelineReplay()
end
assert(timelineReplay, "You need to select a replay")


local pemDir = string.gsub(os.getenv("LOCALAPPDATA"),"\\","/").."/PEM/"
local outdir = pemDir.."IpsLuaExportTest/"
os.execute("mkdir "..string.gsub(outdir, "/","\\"))

local t = os.clock()

local timeline = buildTimeline(timelineReplay)
print("Buildtime: ", os.clock()-t )

local manikinFilenames = {}
for i, family in ipairs(timeline.families) do
  for j, manikin in ipairs(family.manikins) do
    table.insert(manikinFilenames, writeManikinJson(manikin, outdir))
  end
end


print("Exported to ",outdir, ". Time: ", os.clock()-t )

local pemData = {
  src = "IPS",
  parser = "IMMAManikin",
  parserVersion = "0.1",
  manikinFilenames = manikinFilenames,
}

local cmd="curl -X POST http://localhost:5050 --data-binary \""..string.gsub(JSON.encode(pemData), "\"", "\\\"") --.."\" --no-buffer --max-time 5"
print(cmd)
local process = io.popen(cmd)
local resultJson=process:read("*a")
process:close()
print("result:",resultJson)

