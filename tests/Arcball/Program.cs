using FolderVerse;
using Microsoft.Xna.Framework;
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var area=new Rectangle(0,0,400,400);var center=new Point(200,200);
var quarter=Arcball.Drag(Quaternion.Identity,center,new Point(200,0),area);
var inverted=Arcball.Drag(quarter,center,new Point(200,0),area);
Check(Vector3.Distance(Vector3.Transform(Vector3.Backward,quarter),Vector3.Up)<.001f,"Grab follows pointer");
Check(Vector3.Distance(Vector3.Transform(Vector3.Backward,inverted),Vector3.Forward)<.001f,"Rotation crosses pole");
var rolled=Arcball.Drag(Quaternion.Identity,new Point(400,200),new Point(200,0),area);
Check(Vector3.Distance(Vector3.Transform(Vector3.Right,rolled),Vector3.Up)<.001f,"Rim drag rolls");
var restored=Arcball.Drag(quarter,new Point(200,0),center,area);
Check(MathF.Abs(Quaternion.Dot(restored,Quaternion.Identity))>.9999f,"Reverse drag restores pose");
var pose=Quaternion.Identity;for(int i=0;i<10000;i++)pose=Arcball.Drag(pose,new Point(180,210),new Point(204,170),area);
Check(float.IsFinite(pose.X) && MathF.Abs(pose.Length()-1)<.0001f,"Repeated rotation remains normalized");
var opposite=Arcball.Drag(pose,new Point(0,200),new Point(400,200),area);
Check(float.IsFinite(opposite.X) && MathF.Abs(opposite.Length()-1)<.0001f,"Opposite rim points stay finite");
Check(Arcball.Drag(pose,center,center,area)==pose,"Stationary pointer preserves pose");
Console.WriteLine("PASS: Arcball follows pointer, crosses poles, rolls, reverses and stays normalized after 10000 drags.");