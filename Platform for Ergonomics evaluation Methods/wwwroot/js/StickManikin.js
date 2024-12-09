
function StickieBone(thickness, material, j1, j2) {
    var self = new THREE.Mesh(new THREE.CylinderGeometry(thickness, thickness, 1, 32), material);
    self.update = function () {
        var p1 = j1.getWorldPosition(new THREE.Vector3());
        var p2 = j2.getWorldPosition(new THREE.Vector3());
        var dist = p2.clone().sub(p1).length();
        self.scale.set(1, 1, 1);
        self.rotation.set(0, 0, 0);
        self.position.copy(self.parent.worldToLocal(p1.clone()));
        self.scale.set(1, dist, 1);
        self.lookAt(p2);
        self.translateZ(dist*.5);
        self.rotateX(Math.PI/2);
        self.p1=p1;
        self.p2 = p2;

    };
    return self;
}


function parseVec3(obj) {
    return new THREE.Vector3(obj.X, obj.Y, obj.Z);
}
function StickieLimb(name, jointNames, manikin, material) {
    var thickness = .02; // StickManikin.isFinger(joint.name) ? .01 : .02;
    var self = new THREE.Object3D();
    self.name = name;
    manikin.add(self);
    self.bones = [];
    self.joints = [];
    console.log(jointNames);
    var prevJoint = null;
    jointNames.forEach(jointName => {
        var joint = new THREE.Mesh(new THREE.SphereGeometry(thickness, 16, 16), material);
        joint.name = jointName;
        self.add(joint);
        self.joints.push(joint);
        if (prevJoint) {
            var bone = new StickieBone(thickness, material, prevJoint, joint);
            self.bones.push(bone);
            self.add(bone);
        }
        prevJoint = joint;
    });
    

    self.update = function () {
        positions = manikin.getLimbJointPositions(self);
        for (var i = 0; i < positions.length; i++) {
            self.joints[i].position.copy(positions[i]);
        }
        self.updateWorldMatrix(false, true);
        self.bones.forEach(bone => { bone.update(); })
        self.updateWorldMatrix(false, true);
    };
    return self;

}
function StickieHead(material){
    var self=new THREE.Object3D();
    const skull=new THREE.Mesh( new THREE.SphereGeometry( .1,16,16), material);
    skull.name="skull";
    skull.scale.set(1.3,1,1);
    skull.translateX(-.05);
    self.add(skull);
    const eyeMat=new THREE.MeshBasicMaterial( { color: "gray" } );;
    const leftEye=new THREE.Mesh( new THREE.SphereGeometry( .02,16,16), eyeMat);
    const eyeDist=.07;
    leftEye.scale.set(1.2,1,1);
    leftEye.translateX(-.02);
    leftEye.translateZ(.08);
    leftEye.translateY(eyeDist/2);
    skull.add(leftEye);
    const rightEye=leftEye.clone();
    rightEye.translateY(-eyeDist);
    skull.add(rightEye);
    return self;
}




function XYZMarker(){
    var marker=new THREE.Object3D();
    var lineLength=.1;
    var xyzLines=[
        new THREE.Line(new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0,0,0),new THREE.Vector3(lineLength,0,0)]), new THREE.LineBasicMaterial({color:"red"})),
        new THREE.Line(new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0,0,0),new THREE.Vector3(0,lineLength,0)]), new THREE.LineBasicMaterial({color:"green"})),
        new THREE.Line(new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0,0,0),new THREE.Vector3(0,0,lineLength)]), new THREE.LineBasicMaterial({color:"blue"})), 
    ];
    xyzLines.forEach(line=>{
        marker.add(line);
    });
    return marker;
}

//function TestBar(){
//    var self = new THREE.Mesh( new THREE.CylinderGeometry( .1, .1, 1, 32 ),  new THREE.MeshPhongMaterial( { color: "white", specular:0x777777 } ));
//    self.scale.set(.2,1,.2);
//    return self;
//}
class StickManikin extends THREE.Object3D{
    static instances=[];
    static isFinger(name){
        var hints=["Thumb","Index","Middle","Ring","Pinky"];
        for(var i=0;i<hints.length;i++){
            if(name.indexOf(hints[i])>=0){
                return true;
            }
        }
    }
    static getJointSize(jointName){
        return StickManikin.isFinger(jointName)?.01:.02;
    }
    static getLimbMaterial(color){
       return new THREE.MeshPhongMaterial( { color: color, specular:0x777777 } );; 
    }
    
    //__constructor(data, flags) {
    //    super();
    //    if (!data) {
    //        return;
    //    }
    //    StickManikin.instances.push(this);
    //    this.color = new THREE.Color("blue");
    //    this.boneMaterial = StickManikin.getLimbMaterial(this.color);
    //    this.bones = [];
    //    this.jointSet = new JointSet(data, JointSet.HIPS_TO_L5S1);
    //    var joints = this.jointSet.activeJoints;
    //    this.transReader = new TransformReader(data);
    //    joints.forEach(joint => {
    //        if (joint.parentJoint) {
    //            var bone = new StickieBone(this, joint, this.boneMaterial);
    //            joint.bone = bone;
    //            bone.name = joint.name + "Bone";
    //            this.bones.push(bone);
    //        }
    //        this.add(joint);
    //        joint.xyzMarker = new XYZMarker();
    //        joint.xyzMarker.name = "xyzMarker";
    //        joint.add(joint.xyzMarker);
    //        var jointSize = StickManikin.getJointSize(joint.name);
    //        joint.ball = new THREE.Mesh(new THREE.SphereGeometry(jointSize, 16, 16), StickManikin.getLimbMaterial(this.color));
    //        joint.ball.name = "ball";
    //        joint.add(joint.ball);
    //    });
    //    this.head = new StickieHead(this, this.boneMaterial);
    //    this.head.name = "head";
    //    this.joints = joints;
    //    this.showXYZMarkers(false);
    //    this.setFrameIdx(0);
    //    //        this.shoulderBar=new TestBar();
    //    //        this.add(this.shoulderBar);
    //    //this.setOpacity(.2);
    //}

    getLimbJointPositions(limb) {
        var ret = [];
        this.curFrame.limbJointPositions[this.limbs.indexOf(limb)].forEach(p => {
            var v = parseVec3(p);
            this.worldToLocal(v);
            ret.push(v);
        });
        return ret;
    }

    constructor(data, flags) {
        super();
        if (!data) {
            return;
        }
        StickManikin.instances.push(this);
        this.color = new THREE.Color("blue");
        this.boneMaterial = StickManikin.getLimbMaterial(this.color);
        this.limbs = [];
        this.data = JSON.parse(data);
        var limbIdx = 0;
        this.data.limbNames.forEach(name => {
            this.limbs.push(new StickieLimb(name, this.data.limbJoints[limbIdx++], this, this.boneMaterial));
        });

        //this.head = new StickieHead(this, this.boneMaterial);
        //this.head.name = "head";
        this.setFrameIdx(0);
    }
    createClone(){
        var ret=this.clone();
        ret.boneMaterial=this.boneMaterial.clone();
        ret.joints=[];
        ret.color=this.orgColor?this.orgColor:this.color;
        this.joints.forEach(j=>{
            var jc=ret.getObjectByName(j.name);//j.clone();
            jc.jointIdx=j.jointIdx;
            jc.xyzMarker=jc.getObjectByName("xyzMarker");
            jc.ball=jc.getObjectByName("ball");
            jc.ball.material=StickManikin.getLimbMaterial(ret.color);
            jc.bone=ret.getObjectByName(j.name+"Bone");
            if(jc.bone){
                jc.bone.material=ret.boneMaterial;
            }
            ret.joints.push(jc);
        });
        ret.head=ret.getObjectByName("head");
        ret.head.skull=ret.head.getObjectByName("skull");
        ret.head.skull.material=ret.boneMaterial;
        ret.setColor(ret.color);
        return ret;
    }

    update(){
        this.setFrameIdx(this.frameIdx);
    }
    setFrameIdx(idx) {
        this.frameIdx = Math.min(idx, this.data.frames.length - 1);
        this.curFrame = this.data.frames[this.frameIdx];
        this.position.copy(parseVec3(this.curFrame.limbJointPositions[0][0]));
        this.updateWorldMatrix(false, true);
        this.limbs.forEach(limb => { limb.update(); })
        this.updateWorldMatrix(true, true);

        this.lastUpdateTime = Date.now();
        return this.curFrame;

        
    }
    setColor=function(color){
        this.color=new THREE.Color(color);
        this.boneMaterial.color=this.color;
    }
    getJoint(name) {
        return this.getObjectByName(name);
    }
}
