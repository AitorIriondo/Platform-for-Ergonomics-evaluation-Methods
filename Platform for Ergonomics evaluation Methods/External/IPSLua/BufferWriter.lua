
--Heavily inspired by https://stackoverflow.com/questions/18886447/convert-signed-ieee-754-float-to-hexadecimal-representation
local function writeNum32(num, buf, offset)
  if num == 0 then
    buf[offset+1]=0x00
    buf[offset+2]=0x00
    buf[offset+3]=0x00
    buf[offset+4]=0x00
    return
  end
  local sign = 0
  if num < 0 then
    sign = 0x80
    num = -num
  end
  local mantissa, exponent = math.frexp(num)
  if mantissa ~= mantissa then
    buf[offset+1]=0xFF
    buf[offset+2]=0x88
    buf[offset+3]=0x00
    buf[offset+4]=0x00
  elseif mantissa == math.huge or exponent > 0x80 then
    buf[offset+1]= sign==0 and 0x7F or 0xFF
    buf[offset+2]=0x80
    buf[offset+3]=0x00
    buf[offset+4]=0x00
  elseif (mantissa == 0.0 and exponent == 0) or exponent < -0x7E then
    buf[offset+1]=sign
    buf[offset+2]=0x00
    buf[offset+3]=0x00
    buf[offset+4]=0x00
  else
    exponent = exponent + 0x7E
    mantissa = (mantissa * 2.0 - 1.0) * math.ldexp(0.5, 24)
    buf[offset+1]=sign + math.floor(exponent / 0x2)
    buf[offset+2]=(exponent % 0x2) * 0x80 + math.floor(mantissa / 0x10000)
    buf[offset+3]=math.floor(mantissa / 0x100) % 0x100
    buf[offset+4]=mantissa % 0x100
  end  
end


---
--@type BufferWriter
--@field #number offset
--@field #bytes buffer
BufferWriter={}
BufferWriter.__index=BufferWriter

---
--@return #BufferWriter
function BufferWriter.new()
  local self={}
  setmetatable(self,BufferWriter)
  self.buffer={}
  self.offset=0
  return self
end


---
--@param #BufferWriter self
--@param #number num
function BufferWriter:writeInt(num)
  self.buffer[self.offset + 4] = num % 256
  self.buffer[self.offset + 3] = math.floor(num / 256) % 256
  self.buffer[self.offset + 2] = math.floor(num / 65536) % 256
  self.buffer[self.offset + 1] = math.floor(num / 16777216) % 256
  self.offset=self.offset+4
end


---
--@param #BufferWriter self
--@param #number num
function BufferWriter:writeNum(num)
  writeNum32(num,self.buffer,self.offset)
  self.offset=self.offset+4
end

---
--@param #BufferWriter self
function BufferWriter:reset()
  self.offset=0
end

---
--@param #BufferWriter self
function BufferWriter:getBytes()
  return unpack(self.buffer, 1, self.offset)
end

function BufferWriter.unpackFloat32(packed)
    local b1, b2, b3, b4 = string.byte(packed, 1, 4)
    local exponent = (b1 % 0x80) * 0x02 + math.floor(b2 / 0x80)
    local mantissa = math.ldexp(((b2 % 0x80) * 0x100 + b3) * 0x100 + b4, -23)
    if exponent == 0xFF then
        if mantissa > 0 then
            return 0 / 0
        else
            mantissa = math.huge
            exponent = 0x7F
        end
    elseif exponent > 0 then
        mantissa = mantissa + 1
    else
        exponent = exponent + 1
    end
    if b1 >= 0x80 then
        mantissa = -mantissa
    end
    return math.ldexp(mantissa, exponent - 0x7F)
end


---To be treated as constants
local UP=Vector3d(0,0,1)
local LEFT=Vector3d(0,1,0)
local FORWARD=Vector3d(1,0,0)
local ZERO=Vector3d(0,0,0)

---
--@param #BufferWriter self
--@param ips#Vector3d v
function BufferWriter:writeVector3d(v)
  self:writeNum(v.x)
  self:writeNum(v.y)
  self:writeNum(v.z)
end

---
--@param #BufferWriter self
--@param ips#Transf3
function BufferWriter:writePosQuat(trans)
  local pos=trans:transform(ZERO)
  local f=trans:transform(FORWARD)
  local l=trans:transform(LEFT)
  local u=trans:transform(UP)
  self:writeNum(pos.x)
  self:writeNum(pos.y)
  self:writeNum(pos.z)
  local m00=f.x-pos.x
  local m01=l.x-pos.x
  local m02=u.x-pos.x
  local m10=f.y-pos.y
  local m11=l.y-pos.y
  local m12=u.y-pos.y
  local m20=f.z-pos.z
  local m21=l.z-pos.z
  local m22=u.z-pos.z
  local tr = m00 + m11 + m22
  if (tr > 0) then
    local S = math.sqrt(tr+1.0) * 2 -- S=4*qw 
    self:writeNum((m21 - m12) / S)
    self:writeNum((m02 - m20) / S)
    self:writeNum((m10 - m01) / S) 
    self:writeNum(0.25 * S)
  elseif ((m00 > m11) and (m00 > m22)) then
    local S = math.sqrt(1.0 + m00 - m11 - m22) * 2 -- S=4*qx 
    self:writeNum(0.25 * S)
    self:writeNum((m01 + m10) / S) 
    self:writeNum((m02 + m20) / S) 
    self:writeNum((m21 - m12) / S)
  elseif (m11 > m22) then 
    local S = math.sqrt(1.0 + m11 - m00 - m22) * 2 -- S=4*qy
    self:writeNum((m01 + m10) / S) 
    self:writeNum(0.25 * S)
    self:writeNum((m12 + m21) / S) 
    self:writeNum((m02 - m20) / S)
  else
    local S = math.sqrt(1.0 + m22 - m00 - m11) * 2 -- S=4*qz
    self:writeNum((m02 + m20) / S)
    self:writeNum((m12 + m21) / S)
    self:writeNum(0.25 * S)
    self:writeNum((m10 - m01) / S)
  end 
end

