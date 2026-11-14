namespace FolderVerse;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class AnimalToyMesh
{
    private static readonly Color Cream = new(255, 222, 173);
    private static readonly Color Teal = new(0, 137, 115);
    private static readonly Color Gold = new(255, 184, 23);
    private static readonly Color Dark = new(39, 34, 27);
    private readonly List<VertexPositionColorNormal> _vertices = new();

    public static VertexPositionColorNormal[] Create(PlanetHeadSystem animal)
    {
        var mesh = new AnimalToyMesh();
        switch (animal)
        {
            case PlanetHeadSystem.Fish: mesh.Fish(); break;
            case PlanetHeadSystem.Monkey: mesh.Monkey(); break;
            case PlanetHeadSystem.Dog: mesh.Dog(); break;
        }
        if (animal == PlanetHeadSystem.Fish)
        {
            // The fish's head points toward +X, the dog toward +Z.
            var turn = Matrix.CreateRotationY(MathHelper.PiOver2);
            for (int i = 0; i < mesh._vertices.Count; i++)
            {
                var v = mesh._vertices[i];
                mesh._vertices[i] = new(Vector3.Transform(v.Position, turn), v.Color, Vector3.TransformNormal(v.Normal, turn));
            }
        }
        return mesh._vertices.ToArray();
    }

    private void Rounded(Vector3 center, Vector3 radii, Color color, float power = 1, float tilt = 0)
    {
        const int rows = 16, columns = 32;
        var turn = Matrix.CreateRotationZ(tilt);
        float Shape(float x) => MathF.CopySign(MathF.Pow(MathF.Abs(x), power), x);
        (Vector3 Point, Vector3 Normal) At(int row, int column)
        {
            float lat = MathHelper.Pi * row / rows - MathHelper.PiOver2;
            float lon = MathHelper.TwoPi * column / columns;
            var unit = new Vector3(Shape(MathF.Cos(lat)) * Shape(MathF.Cos(lon)), Shape(MathF.Sin(lat)),
                Shape(MathF.Cos(lat)) * Shape(MathF.Sin(lon)));
            var normal = new Vector3(
                MathF.CopySign(MathF.Pow(MathF.Abs(unit.X), 2 / power - 1), unit.X) / radii.X,
                MathF.CopySign(MathF.Pow(MathF.Abs(unit.Y), 2 / power - 1), unit.Y) / radii.Y,
                MathF.CopySign(MathF.Pow(MathF.Abs(unit.Z), 2 / power - 1), unit.Z) / radii.Z);
            return (center + Vector3.Transform(unit * radii, turn), Vector3.Normalize(Vector3.TransformNormal(normal, turn)));
        }
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < columns; col++)
            Quad(At(row, col), At(row, col + 1), At(row + 1, col + 1), At(row + 1, col), color);
    }

    private void Quad((Vector3 P, Vector3 N) a, (Vector3 P, Vector3 N) b,
        (Vector3 P, Vector3 N) c, (Vector3 P, Vector3 N) d, Color color)
    {
        foreach (var v in new[] { a, b, c, a, c, d })
            _vertices.Add(new(v.P, color, v.N));
    }

    private void Tube(Vector3 a, Vector3 b, float radius, Color color)
    {
        var axis = Vector3.Normalize(b - a);
        var rotation = Matrix.Identity;
        var center = (a + b) / 2;
        // A smooth capsule, oriented along the segment.
        int start = _vertices.Count;
        Rounded(Vector3.Zero, new(radius, Vector3.Distance(a, b) / 2 + radius, radius), color, .55f);
        var cross = Vector3.Cross(Vector3.Up, axis);
        if (cross.LengthSquared() > .00001f)
            rotation = Matrix.CreateFromAxisAngle(Vector3.Normalize(cross), MathF.Acos(Math.Clamp(axis.Y, -1, 1)));
        for (int i = start; i < _vertices.Count; i++)
        {
            var v = _vertices[i];
            _vertices[i] = new(center + Vector3.Transform(v.Position, rotation), v.Color, Vector3.TransformNormal(v.Normal, rotation));
        }
    }

    private void Chopsticks(Vector3 at)
    {
        for (int stick = 0; stick < 2; stick++)
        {
            var start = at + new Vector3(stick * .085f, -.25f, 0);
            var end = start + new Vector3(-.16f, .65f, .06f);
            Tube(start, end, .027f, Gold);
            Tube(Vector3.Lerp(start, end, .88f), end, .033f, Teal);
        }
    }

    private void Bowl(Vector3 center)
    {
        Rounded(center + new Vector3(0, -.14f, 0), new(.17f, .035f, .17f), Gold, .55f);
        var profile = new (float Radius, float Height, Color Color)[]
        {
            (.16f,-.13f,Teal),(.20f,-.11f,Teal),(.24f,-.04f,Cream),
            (.26f,.04f,Cream),(.26f,.07f,Teal),(.24f,.085f,Teal),
            (.215f,.065f,Cream),(.20f,-.01f,Cream),(.12f,-.085f,Cream),(0,-.09f,Cream)
        };
        for (int ring = 0; ring < profile.Length - 1; ring++)
        {
            var a = profile[ring];
            var b = profile[ring + 1];
            (Vector3, Vector3) At(float angle, float radius, float height)
            {
                var radial = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
                var normal = Vector3.Normalize(radial * (b.Height - a.Height) - Vector3.Up * (b.Radius - a.Radius));
                return (center + radial * radius + Vector3.Up * height, normal);
            }
            for (int segment = 0; segment < 40; segment++)
            {
                float from = MathHelper.TwoPi * segment / 40, to = MathHelper.TwoPi * (segment + 1) / 40;
                Quad(At(from,a.Radius,a.Height),At(to,a.Radius,a.Height),
                    At(to,b.Radius,b.Height),At(from,b.Radius,b.Height),a.Color);
            }
        }
    }

    private void Monkey()
    {
        Rounded(new(0,-.14f,0),new(.45f,.54f,.32f),Cream);
        Rounded(new(0,-.19f,.27f),new(.21f,.25f,.08f),Teal,.72f);
        Rounded(new(0,.69f,0),new(.58f,.48f,.41f),Teal,.66f);
        Rounded(new(0,.65f,.35f),new(.44f,.33f,.105f),Cream,.72f);
        Rounded(new(0,1.15f,0),new(.25f,.105f,.22f),Gold,.55f);
        Rounded(new(0,.48f,.43f),new(.19f,.13f,.11f),Gold);
        Rounded(new(0,.5f,.53f),new(.051f,.036f,.015f),Dark);
        Rounded(new(0,.433f,.523f),new(.064f,.013f,.018f),Dark);
        for (int side = -1; side <= 1; side += 2)
        {
            Rounded(new(side*.56f,.69f,0),new(.16f,.21f,.125f),Teal,.73f);
            Rounded(new(side*.13f,.91f,.39f),new(.11f,.13f,.075f),Teal,.82f,side*.27f);
            Rounded(new(side*.195f,.67f,.447f),new(.055f,.064f,.027f),Dark);
            Tube(new(side*.22f,-.5f,0),new(side*.28f,-.98f,0),.13f,Cream);
            Rounded(new(side*.28f,-.91f,0),new(.15f,.08f,.15f),Gold,.63f);
            Rounded(new(side*.28f,-1.04f,.07f),new(.19f,.13f,.23f),Teal,.55f);
            Tube(new(side*.35f,.15f,0),new(side*.72f,-.14f,.08f),.11f,Cream);
            Rounded(new(side*.76f,-.12f,.09f),new(.133f,.13f,.13f),Teal);
        }
        Chopsticks(new(-.77f,-.05f,.11f));
        Bowl(new(.81f,-.08f,.1f));
    }

    private void Dog()
    {
        Rounded(new(0,-.04f,-.25f),new(.40f,.38f,.83f),Cream,.53f);
        Rounded(new(0,.13f,.65f),new(.42f,.40f,.34f),Cream,.53f);
        Rounded(new(0,-.01f,.92f),new(.29f,.20f,.2f),Gold,.55f);
        Rounded(new(0,.04f,1.10f),new(.12f,.071f,.046f),Dark,.6f);
        Tube(new(0,.17f,-.94f),new(0,.38f,-1.19f),.115f,Cream);
        Tube(new(0,.38f,-1.19f),new(0,.66f,-1.24f),.11f,Cream);
        Rounded(new(0,.69f,-1.24f),new(.116f,.115f,.112f),Gold,.55f);
        for (int side = -1; side <= 1; side += 2)
        {
            Rounded(new(side*.36f,-.13f,-.31f),new(.081f,.21f,.62f),Teal,.51f);
            Rounded(new(side*.43f,.30f,.64f),new(.15f,.25f,.16f),Teal,.55f,side*.45f);
            Rounded(new(side*.20f,.20f,.975f),new(.054f,.056f,.035f),Dark);
            Leg(side,-.76f,false);
            Leg(side,.49f,side<0);
        }
        Chopsticks(new(-.51f,-.42f,.69f));
        Bowl(new(.60f,-.79f,.56f));

        void Leg(int side,float z,bool raised)
        {
            var foot = new Vector3(side*(raised?.50f:.29f),raised?-.47f:-.82f,z+(raised?.17f:0));
            Tube(new(side*.29f,-.24f,z),foot,.12f,Cream);
            Rounded(foot,new(.15f,.10f,.18f),Teal,.58f);
        }
    }

    private void Fish()
    {
        Rounded(new(0,0,.15f),new(.40f,.53f,.90f),Cream,.82f);
        Rounded(new(0,-.045f,1.00f),new(.215f,.205f,.16f),Gold,.65f);
        Rounded(new(0,.49f,-.06f),new(.10f,.38f,.41f),Teal,.62f);
        Rounded(new(0,0,-.72f),new(.20f,.22f,.18f),Cream,.62f);
        for (int side = -1; side <= 1; side += 2)
        {
            Rounded(new(0,side*.22f,-1.04f),new(.095f,.26f,.37f),Cream,.64f);
            Rounded(new(0,side*.365f,-1.26f),new(.088f,.105f,.13f),Gold,.65f);
            Rounded(new(side*.345f,-.08f,.01f),new(.10f,.22f,.46f),Teal,.57f);
            Rounded(new(side*.319f,.12f,.71f),new(.047f,.06f,.06f),Dark);
            Tube(new(side*.36f,-.12f,.24f),new(side*.55f,-.18f,.47f),.10f,Teal);
            Rounded(new(side*.55f,-.17f,.48f),new(.12f,.12f,.13f),Teal);
        }
        Chopsticks(new(-.57f,-.04f,.49f));
        Bowl(new(.59f,-.09f,.48f));
    }
}
