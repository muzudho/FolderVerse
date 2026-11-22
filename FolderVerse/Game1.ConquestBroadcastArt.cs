namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private readonly Dictionary<string,Texture2D> _broadcastArt=new();
    private Texture2D BroadcastArt(string name)
    {
        if(_broadcastArt.TryGetValue(name,out var image))return image;
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","ConquestBroadcast",name+"-v001.png"));
        image=Texture2D.FromStream(GraphicsDevice,stream);
        var pixels=new Color[image.Width*image.Height];image.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}
        image.SetData(pixels);_broadcastArt.Add(name,image);return image;
    }
    private int BroadcastBoardKind
    {
        get{
            if(!_conquestBroadcast.Outdoor)return 1;
            if(_conquestBroadcast.Location<0)return 0;
            var node=_setup.Nodes.All[_conquestBroadcast.Location];
            if(node.IsHarbor || node.Terrain==TravelTerrain.Sea || _setup.Cells[node.Cell].Face is 2 or 3)return 2;
            return NodeSupervisors.For(node,_setup.Cells[node.Cell].Face) is 1 or 2?1:node.Terrain==TravelTerrain.Mountain?3:0;
        }
    }
    private static readonly string[] BroadcastBoardNames={"board-plywood","board-electronic","board-cart","board-pipe","board-whiteboard"};
    private static readonly Rectangle[] BroadcastBoardFaces={new(166,86,1201,650),new(240,150,1058,600),new(249,117,963,548),new(190,105,1104,625),new(145,65,1120,640)};
    private void DrawBroadcastBoard(int kind,Rectangle face)
    {
        var image=BroadcastArt(BroadcastBoardNames[kind]);var source=BroadcastBoardFaces[kind];
        float scale=face.Width/(float)source.Width;
        int x=face.X-(int)MathF.Round(source.X*scale),width=(int)MathF.Round(image.Width*scale);
        int top=(int)MathF.Round(source.Top*scale),bottom=(int)MathF.Round((image.Height-source.Bottom)*scale);
        // Stretch only straight middle uprights. Wheels, feet and top fittings keep their shape.
        _spriteBatch.Draw(image,new Rectangle(x,face.Y-top,width,top),new Rectangle(0,0,image.Width,source.Top),Color.White);
        _spriteBatch.Draw(image,new Rectangle(x,face.Y,width,face.Height),new Rectangle(0,source.Y,image.Width,source.Height),Color.White);
        _spriteBatch.Draw(image,new Rectangle(x,face.Bottom,width,bottom),new Rectangle(0,source.Bottom,image.Width,image.Height-source.Bottom),Color.White);
    }
    private void DisposeBroadcastArt(){foreach(var art in _broadcastArt.Values)art.Dispose();_broadcastArt.Clear();}
}