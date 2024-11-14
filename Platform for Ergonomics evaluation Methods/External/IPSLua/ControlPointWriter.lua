
local JSON=require("json")
require("BufferWriter")

---
--@type ControlPointsWriter
--@field ips#ManikinFamily family
--@field #number manikinIdx
--@field ips#ManikinControlPointVector controlPoints
--@field BufferWriter#BufferWriter bufferWriter
--@field #file tmpfile
--@field #table meta
ControlPointsWriter={}
ControlPointsWriter.__index=ControlPointsWriter

---
--@param ips#ManikinFamily family
--@param #number manikinIdx
--@return #ManikinBody
function ControlPointsWriter.new(family, filename)
  ---
  --@field #ControlPointsWriter self
  local self={}
  setmetatable(self,ControlPointsWriter)
  self.bufferWriter=BufferWriter.new()
  self.tmpfilename=filename..".tmp"
  self.tmpfile=io.open(self.tmpfilename,"wb")
  self.filename = filename
  self.family=family
  self.controlPoints = family:getControlPoints()
  self.meta={
    familyName=family:getVisualization():getLabel(),
    familyID=family:getID(),
    controlPointIds={},
    controlPointNames={},
    timeSteps={},
  }
  for i=0, self.controlPoints:size()-1 do
    ---
    --@field ips#ManikinControlPoint cp
    local cp = self.controlPoints[i]
    table.insert(self.meta.controlPointNames, cp:getName())
    table.insert(self.meta.controlPointIds, cp:getID())
  end
  return self
end

---
--@param #ControlPointsWriter self 
function ControlPointsWriter:update(timestamp)
  table.insert(self.meta.timeSteps, timestamp)
  self.bufferWriter:reset()
  for i=0, self.controlPoints:size()-1 do
    ---
    --@field ips#ManikinControlPoint cp
    local cp = self.controlPoints[i]
    self.bufferWriter:writePosQuat(cp:getTarget())
    self.bufferWriter:writeVector3d(cp:getResultantContactForce()) 
    self.bufferWriter:writeVector3d(cp:getResultantContactTorque()) 
  end
  self.tmpfile:write(string.char(self.bufferWriter:getBytes()))
end

---
--@param #ControlPointsWriter self 
function ControlPointsWriter:finalize()
  local file=io.open(self.filename,"wb")
  local metaJson = JSON.encode(self.meta)
  self.bufferWriter:reset()
  self.bufferWriter:writeInt(string.len(metaJson))
  file:write(string.char(self.bufferWriter:getBytes()))
  file:write(metaJson)
  self.tmpfile:close()
  self.tmpfile=io.open(self.tmpfilename,"rb")
  local bufSize=4096
  local buf = self.tmpfile:read(bufSize)
  while buf do
    file:write(buf)
    buf = self.tmpfile:read(bufSize)
  end
  self.tmpfile:close()
  os.remove(self.tmpfilename)
  file:close()
end
