local JSON=require("json")
require("BufferWriter")
require("ControlPointsWriter")

local timelineReplay = nil
if Ips.getS then
end
--Ips.getProcessRoot():findFirstExactMatch("Replay - [14:55:40]"):toTimelineReplay()
local peemDir = os.getenv("USERPROFILE").."\\AppData\\Local\\Peem\\"
local tmpDir = peemDir
local outdir = "C:/ergoexporttest/PEMtest/"
local t = os.clock()
local config={
  outdir=outdir
};
FileUtils.mkdir(config.outdir);
local HisIpsDir = os.getenv("HISIPSDIR")
FileUtils.putContents(HisIpsDir.."ergoexportcfg.json", JSON.encode(config));
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
  table.insert(cpws,ControlPointsWriter.new(families[i], outdir..families[i]:getVisualization():getLabel().."_ctrlPts.bin"))
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

