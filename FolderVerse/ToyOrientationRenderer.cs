namespace FolderVerse;
using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// The fish, monkey and dog point their heads along +X, +Y and +Z respectively.
// Apply the globe pose to the whole toy while keeping the axis screws fixed.
public sealed partial class ToyOrientationRenderer:IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly VertexBuffer[] _meshes=new VertexBuffer[3];

    private readonly VertexBuffer _screws;
    private readonly int _screwPrimitives;
    public ToyOrientationRenderer(GraphicsDevice device)
    {
        _device=device;_effect=new BasicEffect(device){VertexColorEnabled=true};
        _effect.EnableDefaultLighting();_effect.AmbientLightColor=new Vector3(.48f);
        _effect.SpecularColor=new Vector3(.18f);_effect.SpecularPower=18;
        var screws=BuildAxisScrews();_screws=new VertexBuffer(device,VertexPositionColorNormal.VertexDeclaration,screws.Length,BufferUsage.WriteOnly);_screws.SetData(screws);_screwPrimitives=screws.Length/3;
        foreach(var kind in Enum.GetValues<PlanetHeadSystem>())
        {
            var vertices=AnimalToyMesh.Create(kind);
            var mesh=new VertexBuffer(device,VertexPositionColorNormal.VertexDeclaration,vertices.Length,BufferUsage.WriteOnly);
            mesh.SetData(vertices);
            _meshes[(int)kind]=mesh;
        }
    }
    public void Draw(PlanetHeadSystem kind,Matrix pose)
    {
        _device.BlendState=BlendState.Opaque;_device.DepthStencilState=DepthStencilState.Default;_device.RasterizerState=RasterizerState.CullNone;

        // An oblique view separates the negative Z screw from the robot silhouette.
        _effect.View=Matrix.CreateLookAt(new Vector3(5,4,5),Vector3.Zero,Vector3.Up);
        float aspect=_device.Viewport.AspectRatio,height=Math.Max(5.2f,5.2f/aspect);
        _effect.Projection=Matrix.CreateOrthographic(height*aspect,height,.1f,100);
        // The screws mark the fixed axes used by the rotation buttons, while the robot rotates inside them.
        _effect.World=Matrix.Identity;_device.SetVertexBuffer(_screws);
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawPrimitives(PrimitiveType.TriangleList,0,_screwPrimitives);}
        _effect.World=pose;
        var mesh=_meshes[(int)kind];
        _device.SetVertexBuffer(mesh);
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawPrimitives(PrimitiveType.TriangleList,0,mesh.VertexCount/3);}
    }
    public void Dispose(){foreach(var mesh in _meshes)mesh.Dispose();_screws.Dispose();_effect.Dispose();}
}
