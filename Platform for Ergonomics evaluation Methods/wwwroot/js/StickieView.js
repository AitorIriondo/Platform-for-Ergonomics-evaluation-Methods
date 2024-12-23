
function StickieView(container, settings){
    var self=this;
    settings=settings?settings:{};
    var doCloneStickie=settings.clone;
    var scene=new THREE.Object3D();
    var camera=new THREE.OrthographicCamera();
    camera.up=new THREE.Vector3(0,0,1);
    var pan={hor:0,ver:0};
    var testMarker = new THREE.Mesh(new THREE.SphereGeometry(.025, 16, 16), new THREE.MeshBasicMaterial({ color: "red" }));
    scene.add(testMarker);
    function setDirty(){
        isDirty=true;
    }
    function setZoom(zoom){
        camera.zoom=zoom;
        camera.updateProjectionMatrix();
        setDirty();
    }
    setZoom(1);
    container.addEventListener("mousemove",function(event){
        if(event.buttons>0){
            var scale=.01;
            pan.ver+=event.movementY*scale;
            pan.hor+=event.movementX*scale;
            isDirty=true;
        }
    });

    scene.add(new THREE.HemisphereLight( 0xffffff, 0x222222, 1.5 ));
    scene.add(new THREE.PointLight( 0x111111, 1));
    var stickie;
    var isDirty;
    var localStickie=null;
    var stickieLastUpdateTime=0;
    var ctrlPanel=container.appendChild(document.createElement("div"));
    ctrlPanel.style.position="absolute";
    ctrlPanel.style.left="280px";
    ctrlPanel.style.width="fit-content";
    ctrlPanel.style.textAlign="left";
    var groupChecks=[];
    ctrlPanel.appendChild(document.createElement("hr"));
    var offset = new THREE.Vector3(100, 100, 100);
    var povSelector = document.createElement("div");
    povSelector.className="povSelector";
    povSelector.style.textAlign="center";
    povSelector.style.backgroundColor="rgba(0,0,0,.1)";
    povSelector.style.position="relative";
    povSelector.style.bottom="1.3rem";
    povSelector.addPov = function (caption, x, y, z) {
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
    povSelector.addPov("Iso", 100, 100, 100);
    povSelector.addPov("F",100,0,0);
    povSelector.addPov("B",-100,0,0);
    povSelector.addPov("L",0,100,0);
    povSelector.addPov("R",0,-100,0);
    povSelector.addPov("T",.0001,0,100);
    povSelector.addPov("B",.0001,0,-100);
    
    var renderer = createRenderer();
    function createRenderer(){
        var containerSize=container.getBoundingClientRect();
        if(containerSize.width>0&&containerSize.height>0){
            renderer = new THREE.WebGLRenderer({antialias:true, alpha:true}); //alpha gör transparent bakgrund
            renderer.setSize(containerSize.width, containerSize.height);
            renderer.domElement.addEventListener("contextmenu", e => e. preventDefault());
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
        stickie = s;
        self.stickie = stickie;
        console.log(scene);
    };
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
                if (localStickie !== stickie) {
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
                var pelvisTmp = new THREE.Object3D();
                try {
                    pelvisTmp.position.copy(stickie.getJoint("RightHip").getWorldPosition(new THREE.Vector3()));
                    pelvisTmp.lookAt(stickie.getJoint("LeftHip").getWorldPosition(new THREE.Vector3()));
                    pelvisTmp.rotation.set(0, 0, pelvisTmp.rotation._y);
                    pelvisTmp.rotateZ(Math.PI);
                }
                catch {
                    console.log("No hips found for POV");
                }
                var pos = stickie.position.clone();
                var offs = offset.clone().applyQuaternion(pelvisTmp.quaternion);
                camera.position.copy(offs.add(pos));
                camera.lookAt(pos);
                testMarker.position.copy(pos);
                camera.translateY(pan.ver/camera.zoom);
                camera.translateX(-pan.hor/camera.zoom);
                stickieLastUpdateTime = stickie.lastUpdateTime;
                scene.updateWorldMatrix(true, true);
                isDirty=false;
            }
            renderer.render( scene, camera );
        }
    };

    self.render();
}
