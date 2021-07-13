# FlexiblePolyhedra
Research project 6/7/2021

# How to use
-Create shape in Fusion360 using meter as base unit. Make each face a seperate component.

 Use join feature where you want hinges to appear. Make the edges fairly small
 
-Export shape as FBX

-Import FBX into Unity -- make sure to enable read/write in import settings

-Drag MyScript.cs onto the FBX

-Drag Contrller.cs onto the camera

-Apply settings and click play.

# Settings
  public bool autoUnpack: enable this if you are exporting straight from fusion360 (reccomended on)
  
  public bool useGravity: if you want a gravitational pull on the faces (reccomended off)
  
  public double hingeTolerance: how close in meters hinge shapes should be to create a hinge (start with around .001)
  
  public double sideArea: approximate calculation of the area on the side of the biggest face (m^2)
  
  public bool updateHingeMotion: WIP keep off

# Controls:
WASD  : Directional movement

ArrowKeys : Camera rotational movement 

Space : Moves camera up per its local Y-axis

Cntrl : Moves camera down per its local Y-axis

Left click : apply force push to normal on face

Shift left click : apply force pull to normal on face 

Right click : stop face from moving

Alt click : remove face

H + click 2 faces: remove shared hinge between them


    
