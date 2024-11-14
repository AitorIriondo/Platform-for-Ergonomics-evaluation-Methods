

local JSON = require("json")
local peemDir = os.getenv("USERPROFILE").."\\AppData\\Local\\Peem\\"
local tmpDir = peemDir.."\\ipscomm_tmp\\"
local function sendData(data)
  local filename = tmpDir..os.time().."_"..os.clock()..".txt"
  print(filename)
  local file = io.open(filename, "w")
  if not file then
    os.execute("mkdir "..peemDir)  
    os.execute("mkdir "..tmpDir)
    file = io.open(filename, "w")
  end
  file:write(string.format("%010d",string.len(data)))
  file:write(data)
  file:close()
end

local peemComm = {
  sendData = sendData,
  test = function()
    something = {
      name = "Nisse",
      age = 100,
      time=os.date("%Y-%m-%d %H:%M:%S")
    }
    sendData(JSON.encode(something))
  end
}

return peemComm