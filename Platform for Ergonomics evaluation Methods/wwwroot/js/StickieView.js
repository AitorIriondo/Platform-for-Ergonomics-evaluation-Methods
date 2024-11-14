
function StickieView(container, settings){
    var self=this;
    settings=settings?settings:{};
    var doCloneStickie=settings.clone;
    var scene=new THREE.Object3D();
    var camera=new THREE.OrthographicCamera();
    camera.up=new THREE.Vector3(0,0,1);
    var focusJoint;
    var hoverJoint;
    var pan={hor:0,ver:0};
    var focusJointMarker=new THREE.Mesh( new THREE.SphereGeometry( .03,16,16), new THREE.MeshBasicMaterial({color:"black"}));
    scene.add(focusJointMarker);
    var hoverJointMarker=new THREE.Mesh( new THREE.SphereGeometry( .03,16,16), new THREE.MeshBasicMaterial({color:"gray"}));
    scene.add(hoverJointMarker);
    function setDirty(){
        isDirty=true;
    }
    function setZoom(zoom){
        camera.zoom=zoom;
        camera.updateProjectionMatrix();
        setDirty();
    }
    function setFocusJoint(joint){
        focusJoint=joint;
        setDirty();
    }
    function getJointFromMouseEvent(evt){
        var raycaster = new THREE.Raycaster();
        var mouse = new THREE.Vector2();
        var br=container.getBoundingClientRect();
        mouse.x = ((evt.clientX-br.x) / br.width ) * 2 - 1;
        mouse.y = -((evt.clientY-br.y) / br.height ) * 2 + 1;
        raycaster.setFromCamera( mouse, camera );
        var ret=null;
        if(localStickie){
            var closestDist=100000;
            localStickie.joints.forEach(j=>{
                if(j.visible && j.ball){
                    var hits=raycaster.intersectObject(j.ball,true);
                    if(hits.length>0){
                        var dist=j.position.distanceTo(camera.position);
                        if(dist<closestDist){
                            ret=j;
                            closestDist=dist;
                        }
                    }
                }
            });
        }
        return ret;
    };
    setZoom(1);
    container.addEventListener("mousemove",function(event){
        if(event.buttons>0){
            var scale=.01;
            pan.ver+=event.movementY*scale;
            pan.hor+=event.movementX*scale;
            isDirty=true;
        }
        else{
            var j=getJointFromMouseEvent(event);
            if(j!==hoverJoint){
                isDirty=true;
                container.title=j?j.name:"";
            }
            hoverJoint=j;
        }
    });

    function onRendererClick(evt){
        var j=getJointFromMouseEvent(evt);
        if(j){
            setFocusJoint(j);
            pan.ver=0;
            pan.hor=0;
        }
    };
    
    scene.add(new THREE.HemisphereLight( 0xffffff, 0x222222, 1.5 ));
    scene.add(new THREE.PointLight( 0x111111, 1));
    var stickie;
    var isDirty;
    var localStickie=null;
    var stickieLastUpdateTime=0;
    var offset=new THREE.Vector3(100,0,0);
    var ctrlPanel=container.appendChild(document.createElement("div"));
    ctrlPanel.style.position="absolute";
    ctrlPanel.style.left="280px";
    ctrlPanel.style.width="fit-content";
    ctrlPanel.style.textAlign="left";
    var jointLabel=ctrlPanel.appendChild(document.createElement("div"));
    var groupChecks=[];
    JointGroups.instances.forEach(jointGroup=>{
        var chk=ctrlPanel.appendChild(UIElements.createCheckbox(jointGroup.name,jointGroup.name.indexOf("hand")<0,setDirty));
        chk.style.whiteSpace="nowrap";
        chk.jointGroup=jointGroup;
        groupChecks.push(chk);
    });
    ctrlPanel.appendChild(document.createElement("hr"));
    var chkXYZ=ctrlPanel.appendChild(UIElements.createCheckbox("XYZ Markers",true,setDirty));
    var povSelector=document.createElement("div");
    povSelector.className="povSelector";
    povSelector.style.textAlign="center";
    povSelector.style.backgroundColor="rgba(0,0,0,.1)";
    povSelector.style.position="relative";
    povSelector.style.bottom="1.3rem";
    povSelector.addPov=function(caption,x,y,z){
        var btn=this.appendChild(document.createElement("button"));
        btn.style.padding="0 2px";
        btn.style.margin="0 2px";
        btn.style.cursor="pointer";
        btn.style.fontSize=".8rem";
        btn.innerHTML=caption;
        btn.onclick=function(){
            offset=new THREE.Vector3(x,y,z);
            isDirty=true;
        };
        return btn;
    };
    povSelector.addPov("F",100,0,0);
    povSelector.addPov("B",-100,0,0);
    povSelector.addPov("L",0,100,0);
    povSelector.addPov("R",0,-100,0);
    povSelector.addPov("T",.0001,0,100);
    povSelector.addPov("B",.0001,0,-100);
    var isos=[
        new THREE.Vector3(100,100,100),
        new THREE.Vector3(100,-100,100),
        new THREE.Vector3(-100,-100,100),
        new THREE.Vector3(-100,100,100),
    ];
    povSelector.addPov("Iso",-100,100,100).onclick=function(){
        this.isoIdx=this.isoIdx>=0?++this.isoIdx:0;
        offset=isos[this.isoIdx%isos.length];
        isDirty=true;
    };
    
    var renderer = createRenderer();
    function createRenderer(){
        var containerSize=container.getBoundingClientRect();
        if(containerSize.width>0&&containerSize.height>0){
            renderer = new THREE.WebGLRenderer({antialias:true, alpha:true}); //alpha gör transparent bakgrund
            renderer.setSize(containerSize.width, containerSize.height);
            renderer.domElement.addEventListener("contextmenu", e => e. preventDefault());
            renderer.domElement.addEventListener("click",onRendererClick);
            renderer.domElement.addEventListener("wheel",function(event){
                var amount=1.5;
                if(event.deltaY<0){
                    setZoom(camera.zoom*amount);
                }
                else{
                    setZoom(camera.zoom/amount);
                }
                event.preventDefault();
            });
            
            container.appendChild(renderer.domElement);
            container.appendChild(povSelector);
            return renderer;
        }
    }
    self.setStickie=function(s){
        isDirty=true;
        if(!doCloneStickie){
            scene.remove(stickie);
            scene.add(s);
            localStickie=s;
        }
        stickie=s;
    };
    function getDiffRot(jointName1, jointName2){
        var rot1=localStickie.getJoint(jointName1).quaternion.clone().normalize();
        var rot2=localStickie.getJoint(jointName2).quaternion.clone().normalize();
        var ret=rot1.multiply(rot2.invert()).normalize();
        //console.log({rot1:rot1,rot2:rot2,ret:ret});
        return ret;
    }
    function getDiffEuler(jointName1, jointName2){
        return new Euler(0,0,0).setFromQuaternion(getDiffRot(jointName1, jointName2));
    }
    self.setDirty=function(){
        isDirty=true;
    }
    self.render=function(){
        requestAnimationFrame(self.render);
        if(!renderer && !createRenderer()){
            return;
        }
        if(stickie){
            isDirty|=(!localStickie || stickie.lastUpdateTime!=stickieLastUpdateTime);
            if(isDirty){
                if(localStickie!==stickie){
                    scene.remove(localStickie);
                    localStickie=stickie.createClone();
                    scene.add(localStickie);
                    if(focusJoint){
                        focusJoint=localStickie.getJoint(focusJoint.name);
                    }
                    if(hoverJoint){
                        hoverJoint=localStickie.getJoint(hoverJoint.name);
                    }
                }
                localStickie.highlight(false);
                localStickie.setOpacity(.2);
                localStickie.joints.forEach(j=>{
                    if(j.ball){
                        j.ball.material.opacity=1;
                    }
                });
                
                groupChecks.forEach(chk=>{
                    localStickie.showJointGroup(chk.jointGroup, chk.chk.checked).forEach(j=>{
                       j.xyzMarker.visible=chkXYZ.chk.checked?true:false;
                   });
                });
                if(!focusJoint){
                    focusJoint=localStickie.getJoint("L5S1");
                }
//                var spineRot=getDiffEuler("L5S1","T1T2");
//                console.log("spineRot",spineRot);
//                console.log("twist",spineRot.z);
//                console.log("bend",spineRot.y);

                jointLabel.innerHTML=focusJoint.name;
                var rot=stickie.bodyTrans.rot.clone();
                var pos=focusJoint.position.clone();
                pos.add(stickie.bodyTrans.pos);
                focusJointMarker.position.copy(pos);
                focusJointMarker.updateMatrixWorld(true);
                if(hoverJoint){
                    hoverJointMarker.position.copy(hoverJoint.position.clone().add(stickie.bodyTrans.pos));
                    hoverJointMarker.updateMatrixWorld(true);
                }
                hoverJointMarker.visible=(hoverJoint&&hoverJoint!=focusJoint)?true:false;
                //camera.setRotationFromQuaternion(rot);
                //console.log(camera.rotation.x,camera.rotation.y,camera.rotation.z);
                //camera.rotation.set(camera.rotation.x,camera.rotation.y,0);
                //camera.setRotationFromAxisAngle(new Vector(0,0,1),)
                var offs=offset.clone().applyQuaternion(rot);
                camera.position.copy(offs.add(pos));
                camera.lookAt(pos);
                camera.translateY(pan.ver/camera.zoom);
                camera.translateX(-pan.hor/camera.zoom);
                stickieLastUpdateTime=stickie.lastUpdateTime;
                scene.updateMatrix();
                isDirty=false;
            }
            renderer.render( scene, camera );
        }
    };

    self.render();
}
