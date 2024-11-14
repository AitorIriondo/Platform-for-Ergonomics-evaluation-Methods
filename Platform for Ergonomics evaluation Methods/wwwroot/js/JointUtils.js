class TransformReader{
    constructor(data){
        this.vals=data.vals;
        this.valsPerFrame=data.valsPerFrame;
        this.frameCnt=data.vals.length/data.valsPerFrame;
        this.rotType=data.rotType;
    }
    getTransform(frameIdx){
        return this._getTransform(frameIdx*this.valsPerFrame);
    }
    _getTransform(valOffset){
        return {
            pos:new THREE.Vector3(this.vals[valOffset++], this.vals[valOffset++], this.vals[valOffset++]),
            rot:new THREE.Quaternion(this.vals[valOffset++], this.vals[valOffset++], this.vals[valOffset++], this.vals[valOffset++])
        };
        
    }
}


/**
 * 
 * @type JointTransform
 * @field string name
 * @property {Joint} baseJoint
 */
class JointTransform extends THREE.Object3D{
    constructor(name, parentName){
        super();
        this.name=name;
        this.parentName=parentName;
    }
    applyTransform(frameIdx, transformReader){
        var trans=transformReader._getTransform(frameIdx*transformReader.valsPerFrame+this.posValOffset);
        this.position.copy(trans.pos);
        if(this.rotValOffset){
            this.quaternion.copy(trans.rot);
        }
    }
}
/**
 * 
 * @type JointSet
 * @field JointTransform[] all
 */

class JointSet{
    static HIPS_TO_L5S1=1;
    constructor(data, flags) {
        var all=[
            this.L5S1=new JointTransform("L5S1","IMMASkeleton_female"),
            this.L3L4=new JointTransform("L3L4","L5S1"),
            this.T12L1=new JointTransform("T12L1","L3L4"),
            this.T6T7=new JointTransform("T6T7","T12L1"),
            this.T1T2=new JointTransform("T1T2","T6T7"),
            this.C6C7=new JointTransform("C6C7","T1T2"),
            this.C4C5=new JointTransform("C4C5","C6C7"),
            this.AtlantoAxial=new JointTransform("AtlantoAxial","C4C5"),
            this.RightSC=new JointTransform("RightSC","T1T2"),
            this.RightAC=new JointTransform("RightAC","RightSC"),
            this.RightGH=new JointTransform("RightGH","RightAC"),
            this.RightShoulderRotation=new JointTransform("RightShoulderRotation","RightGH"),
            this.RightElbow=new JointTransform("RightElbow","RightShoulderRotation"),
            this.RightWristRotation=new JointTransform("RightWristRotation","RightElbow"),
            this.RightWrist=new JointTransform("RightWrist","RightWristRotation"),
            this.Right_IndexCarpal=new JointTransform("Right_IndexCarpal","RightWrist"),
            this.Right_IndexProximal=new JointTransform("Right_IndexProximal","Right_IndexCarpal"),
            this.Right_IndexIntermediate=new JointTransform("Right_IndexIntermediate","Right_IndexProximal"),
            this.Right_IndexDistal=new JointTransform("Right_IndexDistal","Right_IndexIntermediate"),
            this.Right_MiddleCarpal=new JointTransform("Right_MiddleCarpal","RightWrist"),
            this.Right_MiddleProximal=new JointTransform("Right_MiddleProximal","Right_MiddleCarpal"),
            this.Right_MiddleIntermediate=new JointTransform("Right_MiddleIntermediate","Right_MiddleProximal"),
            this.Right_MiddleDistal=new JointTransform("Right_MiddleDistal","Right_MiddleIntermediate"),
            this.Right_RingCarpal=new JointTransform("Right_RingCarpal","RightWrist"),
            this.Right_RingProximal=new JointTransform("Right_RingProximal","Right_RingCarpal"),
            this.Right_RingIntermediate=new JointTransform("Right_RingIntermediate","Right_RingProximal"),
            this.Right_RingDistal=new JointTransform("Right_RingDistal","Right_RingIntermediate"),
            this.Right_PinkyCarpal=new JointTransform("Right_PinkyCarpal","RightWrist"),
            this.Right_PinkyProximal=new JointTransform("Right_PinkyProximal","Right_PinkyCarpal"),
            this.Right_PinkyIntermediate=new JointTransform("Right_PinkyIntermediate","Right_PinkyProximal"),
            this.Right_PinkyDistal=new JointTransform("Right_PinkyDistal","Right_PinkyIntermediate"),
            this.Right_ThumbProximal=new JointTransform("Right_ThumbProximal","RightWrist"),
            this.Right_ThumbIntermediate=new JointTransform("Right_ThumbIntermediate","Right_ThumbProximal"),
            this.Right_ThumbDistal=new JointTransform("Right_ThumbDistal","Right_ThumbIntermediate"),
            this.LeftSC=new JointTransform("LeftSC","T1T2"),
            this.LeftAC=new JointTransform("LeftAC","LeftSC"),
            this.LeftGH=new JointTransform("LeftGH","LeftAC"),
            this.LeftShoulderRotation=new JointTransform("LeftShoulderRotation","LeftGH"),
            this.LeftElbow=new JointTransform("LeftElbow","LeftShoulderRotation"),
            this.LeftWristRotation=new JointTransform("LeftWristRotation","LeftElbow"),
            this.LeftWrist=new JointTransform("LeftWrist","LeftWristRotation"),
            this.Left_IndexCarpal=new JointTransform("Left_IndexCarpal","LeftWrist"),
            this.Left_IndexProximal=new JointTransform("Left_IndexProximal","Left_IndexCarpal"),
            this.Left_IndexIntermediate=new JointTransform("Left_IndexIntermediate","Left_IndexProximal"),
            this.Left_IndexDistal=new JointTransform("Left_IndexDistal","Left_IndexIntermediate"),
            this.Left_MiddleCarpal=new JointTransform("Left_MiddleCarpal","LeftWrist"),
            this.Left_MiddleProximal=new JointTransform("Left_MiddleProximal","Left_MiddleCarpal"),
            this.Left_MiddleIntermediate=new JointTransform("Left_MiddleIntermediate","Left_MiddleProximal"),
            this.Left_MiddleDistal=new JointTransform("Left_MiddleDistal","Left_MiddleIntermediate"),
            this.Left_RingCarpal=new JointTransform("Left_RingCarpal","LeftWrist"),
            this.Left_RingProximal=new JointTransform("Left_RingProximal","Left_RingCarpal"),
            this.Left_RingIntermediate=new JointTransform("Left_RingIntermediate","Left_RingProximal"),
            this.Left_RingDistal=new JointTransform("Left_RingDistal","Left_RingIntermediate"),
            this.Left_PinkyCarpal=new JointTransform("Left_PinkyCarpal","LeftWrist"),
            this.Left_PinkyProximal=new JointTransform("Left_PinkyProximal","Left_PinkyCarpal"),
            this.Left_PinkyIntermediate=new JointTransform("Left_PinkyIntermediate","Left_PinkyProximal"),
            this.Left_PinkyDistal=new JointTransform("Left_PinkyDistal","Left_PinkyIntermediate"),
            this.Left_ThumbProximal=new JointTransform("Left_ThumbProximal","LeftWrist"),
            this.Left_ThumbIntermediate=new JointTransform("Left_ThumbIntermediate","Left_ThumbProximal"),
            this.Left_ThumbDistal=new JointTransform("Left_ThumbDistal","Left_ThumbIntermediate"),
            this.LeftHip=new JointTransform("LeftHip","IMMASkeleton_female"),
            this.LeftKnee=new JointTransform("LeftKnee","LeftHip"),
            this.LeftAnkleRot=new JointTransform("LeftAnkleRot","LeftKnee"),
            this.LeftAnkle=new JointTransform("LeftAnkle","LeftAnkleRot"),
            this.LeftToes=new JointTransform("LeftToes","LeftAnkle"),
            this.RightHip=new JointTransform("RightHip","IMMASkeleton_female"),
            this.RightKnee=new JointTransform("RightKnee","RightHip"),
            this.RightAnkleRot=new JointTransform("RightAnkleRot","RightKnee"),
            this.RightAnkle=new JointTransform("RightAnkle","RightAnkleRot"),
            this.RightToes=new JointTransform("RightToes","RightAnkle"),
        ];
        this.all=[];
        all.forEach(jt=>{
            var joint=Joints.getJointByName(jt.name);
            jt.jointIdx=joint.idx;
            this.all.push(jt);
        });
        function getByName(name){
            for(var i=0;i<all.length;i++){
                if(all[i].name==name){
                    return all[i];
                }
            }
        }
        
        var activeJoints=[];
        if(data){
            data.jointMetas.forEach(meta=>{
                var jt=getByName(meta.name);
                if(jt){
                    jt.posValOffset=meta.posValOffset;
                    jt.rotValOffset=meta.rotValOffset;
                    activeJoints.push(jt);
                }
            });
        }
        function getActiveParent(jt){
            var parent=getByName(jt.parentName);
            while(parent){
                if(activeJoints.indexOf(parent)>=0){
                    return parent;
                }
                parent=getByName(parent.parentName);
            }
        }
        var L5S1=this.L5S1;
        this.all.forEach(jt=>{
            if(!jt.parentJoint){
                if(flags&JointSet.HIPS_TO_L5S1 && jt.name.indexOf("Hip")>=0){
                    jt.parentJoint=L5S1;
                }
                else{
                    jt.parentJoint=getActiveParent(jt);
                }
            }
        });
        this.activeJoints=activeJoints;
    };
};

