local JSON=require("json")
require("BufferWriter")
require("ControlPointsWriter")

local timelineReplay = nil
if Ips.getSelection() then
  timelineReplay = Ips.getSelection():toTimelineReplay()
end
assert(timelineReplay, "You need to select a replay")
  
local pemDir = string.gsub(os.getenv("LOCALAPPDATA"),"\\","/").."/PEM/"
local outdir = pemDir.."IpsErgoExportTest/"
local t = os.clock()
local config={
  outdir=outdir
};
os.execute("mkdir "..string.gsub(config.outdir, "/","\\"))

---
--@return #string filename
local function putFileContents(filename, contents)
  local f=io.open(filename,"w")
  f:write(contents)
  f:close()
  return filename
end

putFileContents(os.getenv("TEMP").."/immaergoexportcfg.json", JSON.encode(config));
timelineReplay:computeErgonomicScore("Exporttest",0,timelineReplay:getFinalTime())

---
--@param ips#TimelineReplay replay
--@return #list<ips#ManikinFamily>
local function getActingFamilies(replay)
  local os=replay:getParent():toOperationSequence()
  local actors=os:getActors()
  local ret={}
  for i=0, actors:size()-1 do
    if actors[i]:getTreeObject() then
      local famvis=actors[i]:getTreeObject():toManikinFamilyVisualization()
      if famvis then
        table.insert(ret,famvis:getManikinFamily())
      end
    end
  end
  return ret
end

local families = getActingFamilies(timelineReplay)


---
--@field #list<#ControlPointsWriter>
local cpws={}
for i=1, #families do
  table.insert(cpws,ControlPointsWriter.new(families[i], outdir..families[i]:getVisualization():getLabel()..".ctrlpts"))
end

local dur=timelineReplay:getFinalTime()
for t=0, dur, .03 do
  timelineReplay:setTime(t)
  for i=1, #cpws do
    cpws[i]:update(t)
  end
end

for i=1, #cpws do
  cpws[i]:finalize()
end


print("Exported to ",config.outdir, ". Time: ", os.clock()-t )
local pemData = {
  src = "IPS",
  type = "ManikinTimeline",
  dir=outdir
}

local cmd="curl -X POST http://127.0.0.1:5000 --data-binary \""..string.gsub(JSON.encode(pemData), "\"", "\\\"").."\" --no-buffer --max-time 1" 
print(cmd)
local process = io.popen(cmd)
local resultJson=process:read("*a")
process:close()
print(resultJson)
