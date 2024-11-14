/**
 * @typedef Joint
 * @type {object}
 * @property {string} name
 * @property {number} idx
 * @property {number} numAngles
 * @property {number} firstAngleIdx
 */

/**
 * 
 * @param {type} obj
 * @returns {Joint}
 */
function Joint(obj){
    return obj;
}
var Joints={
    L5S1:new Joint({"name":"L5S1","idx":2,"numAngles":3,"firstAngleIdx":7}),
    L3L4:new Joint({"name":"L3L4","idx":3,"numAngles":3,"firstAngleIdx":10}),
    T12L1:new Joint({"name":"T12L1","idx":4,"numAngles":3,"firstAngleIdx":13}),
    T6T7:new Joint({"name":"T6T7","idx":5,"numAngles":3,"firstAngleIdx":16}),
    T1T2:new Joint({"name":"T1T2","idx":6,"numAngles":3,"firstAngleIdx":19}),
    C6C7:new Joint({"name":"C6C7","idx":7,"numAngles":3,"firstAngleIdx":22}),
    C4C5:new Joint({"name":"C4C5","idx":8,"numAngles":3,"firstAngleIdx":25}),
    AtlantoAxial:new Joint({"name":"AtlantoAxial","idx":9,"numAngles":3,"firstAngleIdx":28}),
    Eyeside:new Joint({"name":"Eyeside","idx":10,"numAngles":2,"firstAngleIdx":31}),
    LeftHip:new Joint({"name":"LeftHip","idx":11,"numAngles":3,"firstAngleIdx":33}),
    LeftKnee:new Joint({"name":"LeftKnee","idx":12,"numAngles":1,"firstAngleIdx":36}),
    LeftAnkleRot:new Joint({"name":"LeftAnkleRot","idx":13,"numAngles":1,"firstAngleIdx":37}),
    LeftAnkle:new Joint({"name":"LeftAnkle","idx":14,"numAngles":2,"firstAngleIdx":38}),
    LeftToes:new Joint({"name":"LeftToes","idx":15,"numAngles":1,"firstAngleIdx":40}),
    RightHip:new Joint({"name":"RightHip","idx":16,"numAngles":3,"firstAngleIdx":41}),
    RightKnee:new Joint({"name":"RightKnee","idx":17,"numAngles":1,"firstAngleIdx":44}),
    RightAnkleRot:new Joint({"name":"RightAnkleRot","idx":18,"numAngles":1,"firstAngleIdx":45}),
    RightAnkle:new Joint({"name":"RightAnkle","idx":19,"numAngles":2,"firstAngleIdx":46}),
    RightToes:new Joint({"name":"RightToes","idx":20,"numAngles":1,"firstAngleIdx":48}),
    RightSC:new Joint({"name":"RightSC","idx":21,"numAngles":2,"firstAngleIdx":49}),
    RightAC:new Joint({"name":"RightAC","idx":22,"numAngles":2,"firstAngleIdx":51}),
    RightGH:new Joint({"name":"RightGH","idx":23,"numAngles":2,"firstAngleIdx":53}),
    RightShoulderRotation:new Joint({"name":"RightShoulderRotation","idx":24,"numAngles":1,"firstAngleIdx":55}),
    RightElbow:new Joint({"name":"RightElbow","idx":25,"numAngles":1,"firstAngleIdx":56}),
    RightWristRotation:new Joint({"name":"RightWristRotation","idx":26,"numAngles":1,"firstAngleIdx":57}),
    LeftSC:new Joint({"name":"LeftSC","idx":27,"numAngles":2,"firstAngleIdx":58}),
    LeftAC:new Joint({"name":"LeftAC","idx":28,"numAngles":2,"firstAngleIdx":60}),
    LeftGH:new Joint({"name":"LeftGH","idx":29,"numAngles":2,"firstAngleIdx":62}),
    LeftShoulderRotation:new Joint({"name":"LeftShoulderRotation","idx":30,"numAngles":1,"firstAngleIdx":64}),
    LeftElbow:new Joint({"name":"LeftElbow","idx":31,"numAngles":1,"firstAngleIdx":65}),
    LeftWristRotation:new Joint({"name":"LeftWristRotation","idx":32,"numAngles":1,"firstAngleIdx":66}),
    LeftWrist:new Joint({"name":"LeftWrist","idx":33,"numAngles":2,"firstAngleIdx":67}),
    Left_IndexCarpal:new Joint({"name":"Left_IndexCarpal","idx":34,"numAngles":3,"firstAngleIdx":69}),
    Left_IndexProximal:new Joint({"name":"Left_IndexProximal","idx":35,"numAngles":2,"firstAngleIdx":72}),
    Left_IndexIntermediate:new Joint({"name":"Left_IndexIntermediate","idx":36,"numAngles":1,"firstAngleIdx":74}),
    Left_IndexDistal:new Joint({"name":"Left_IndexDistal","idx":37,"numAngles":1,"firstAngleIdx":75}),
    Left_MiddleCarpal:new Joint({"name":"Left_MiddleCarpal","idx":38,"numAngles":3,"firstAngleIdx":76}),
    Left_MiddleProximal:new Joint({"name":"Left_MiddleProximal","idx":39,"numAngles":2,"firstAngleIdx":79}),
    Left_MiddleIntermediate:new Joint({"name":"Left_MiddleIntermediate","idx":40,"numAngles":1,"firstAngleIdx":81}),
    Left_MiddleDistal:new Joint({"name":"Left_MiddleDistal","idx":41,"numAngles":1,"firstAngleIdx":82}),
    Left_RingCarpal:new Joint({"name":"Left_RingCarpal","idx":42,"numAngles":3,"firstAngleIdx":83}),
    Left_RingProximal:new Joint({"name":"Left_RingProximal","idx":43,"numAngles":2,"firstAngleIdx":86}),
    Left_RingIntermediate:new Joint({"name":"Left_RingIntermediate","idx":44,"numAngles":1,"firstAngleIdx":88}),
    Left_RingDistal:new Joint({"name":"Left_RingDistal","idx":45,"numAngles":1,"firstAngleIdx":89}),
    Left_PinkyCarpal:new Joint({"name":"Left_PinkyCarpal","idx":46,"numAngles":3,"firstAngleIdx":90}),
    Left_PinkyProximal:new Joint({"name":"Left_PinkyProximal","idx":47,"numAngles":2,"firstAngleIdx":93}),
    Left_PinkyIntermediate:new Joint({"name":"Left_PinkyIntermediate","idx":48,"numAngles":1,"firstAngleIdx":95}),
    Left_PinkyDistal:new Joint({"name":"Left_PinkyDistal","idx":49,"numAngles":1,"firstAngleIdx":96}),
    Left_ThumbProximal:new Joint({"name":"Left_ThumbProximal","idx":50,"numAngles":2,"firstAngleIdx":97}),
    Left_ThumbIntermediate:new Joint({"name":"Left_ThumbIntermediate","idx":51,"numAngles":1,"firstAngleIdx":99}),
    Left_ThumbDistal:new Joint({"name":"Left_ThumbDistal","idx":52,"numAngles":1,"firstAngleIdx":100}),
    RightWrist:new Joint({"name":"RightWrist","idx":53,"numAngles":2,"firstAngleIdx":101}),
    Right_IndexCarpal:new Joint({"name":"Right_IndexCarpal","idx":54,"numAngles":3,"firstAngleIdx":103}),
    Right_IndexProximal:new Joint({"name":"Right_IndexProximal","idx":55,"numAngles":2,"firstAngleIdx":106}),
    Right_IndexIntermediate:new Joint({"name":"Right_IndexIntermediate","idx":56,"numAngles":1,"firstAngleIdx":108}),
    Right_IndexDistal:new Joint({"name":"Right_IndexDistal","idx":57,"numAngles":1,"firstAngleIdx":109}),
    Right_MiddleCarpal:new Joint({"name":"Right_MiddleCarpal","idx":58,"numAngles":3,"firstAngleIdx":110}),
    Right_MiddleProximal:new Joint({"name":"Right_MiddleProximal","idx":59,"numAngles":2,"firstAngleIdx":113}),
    Right_MiddleIntermediate:new Joint({"name":"Right_MiddleIntermediate","idx":60,"numAngles":1,"firstAngleIdx":115}),
    Right_MiddleDistal:new Joint({"name":"Right_MiddleDistal","idx":61,"numAngles":1,"firstAngleIdx":116}),
    Right_RingCarpal:new Joint({"name":"Right_RingCarpal","idx":62,"numAngles":3,"firstAngleIdx":117}),
    Right_RingProximal:new Joint({"name":"Right_RingProximal","idx":63,"numAngles":2,"firstAngleIdx":120}),
    Right_RingIntermediate:new Joint({"name":"Right_RingIntermediate","idx":64,"numAngles":1,"firstAngleIdx":122}),
    Right_RingDistal:new Joint({"name":"Right_RingDistal","idx":65,"numAngles":1,"firstAngleIdx":123}),
    Right_PinkyCarpal:new Joint({"name":"Right_PinkyCarpal","idx":66,"numAngles":3,"firstAngleIdx":124}),
    Right_PinkyProximal:new Joint({"name":"Right_PinkyProximal","idx":67,"numAngles":2,"firstAngleIdx":127}),
    Right_PinkyIntermediate:new Joint({"name":"Right_PinkyIntermediate","idx":68,"numAngles":1,"firstAngleIdx":129}),
    Right_PinkyDistal:new Joint({"name":"Right_PinkyDistal","idx":69,"numAngles":1,"firstAngleIdx":130}),
    Right_ThumbProximal:new Joint({"name":"Right_ThumbProximal","idx":70,"numAngles":2,"firstAngleIdx":131}),
    Right_ThumbIntermediate:new Joint({"name":"Right_ThumbIntermediate","idx":71,"numAngles":1,"firstAngleIdx":133}),
    Right_ThumbDistal:new Joint({"name":"Right_ThumbDistal","idx":72,"numAngles":1,"firstAngleIdx":134}),
    
    getJointByIdx:function(idx){
        return Joints.instances[idx];
    },
    getJointByName:function(name){
        var ret=null;
        Joints.instances.forEach(j=>{
            if(j.name==name){
                ret=j;
            }
        });
        return ret;
    }
    
};
Joints.instances=[];
for (const [key, joint] of Object.entries(Joints)) {
    if(joint.numAngles){
        Joints.instances[joint.idx]=joint;
    }
}

/**
 * @typedef {object} JointGroup
 * @property {string} name
 * @property {Joint} joints
 */

/**
 * 
 * @param {type} obj
 * @returns {JointGroup}
 */
function JointGroup(obj){
    return obj;
}
var JointGroups={
    spine:new JointGroup({name:"Spine",jointIndices:[2,3,4,5,6,7,8,9,10]}),
    leftLeg:new JointGroup({name:"Left leg",jointIndices:[11,12,13,14,15]}),
    rightLeg:new JointGroup({name:"Right leg",jointIndices:[16,17,18,19,20]}),
    leftArm:new JointGroup({name:"Left arm",jointIndices:[27,28,29,30,31,32,33]}),
    rightArm:new JointGroup({name:"Right arm",jointIndices:[21,22,23,24,25,26,53]}),
    leftHand:new JointGroup({name:"Left hand",jointIndices:[34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52]}),
    rightHand:new JointGroup({name:"Right hand",jointIndices:[54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72]})
};

JointGroups.instances=[];
for (const [key, group] of Object.entries(JointGroups)) {
    if(group.jointIndices){
        JointGroups.instances.push(group);
        group.joints=[];
        group.jointIndices.forEach(idx=>{
            group.joints.push(Joints.getJointByIdx(idx));
        });
    }
}

