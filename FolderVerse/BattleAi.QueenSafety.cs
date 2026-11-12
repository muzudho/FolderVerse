namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static partial class BattleAi
{
    internal static bool ProtectQueen(BattleState battle, BattleUnit unit) =>
        unit.Role == BattleRole.Queen && (battle.IsFactionRoot?.Invoke(unit.Owner) ?? true) &&
        (battle.Lessons[unit.Owner] & BattleLesson.QueenCaptured) != 0;

    private static BattleAction ChooseProtectedQueen(BattleState battle, BattleUnit queen, BattleUnit[] enemies,
        bool securingFlag)
    {
        var routes = QueenEscapeRoutes(battle, queen, enemies);
        var facing = enemies.Length == 0 ? queen.Facing : Face(enemies[0].Position - queen.Position);
        if (!routes.ContainsKey(queen.Position))
        {
            queen.Retreating = true;
            return new(Point.Zero, facing); // ClearPaths chooses the retreat step.
        }

        var targets = enemies.Where(enemy => enemy.Role != BattleRole.Queen).ToArray();
        var queue = new Queue<(Point At, Point Step)>();
        var visited = new HashSet<Point> { queen.Position };
        queue.Enqueue((queen.Position, Point.Zero));
        while (queue.TryDequeue(out var path))
        {
            if (securingFlag && path.At == battle.Flag)
                return new(path.Step, path.Step == Point.Zero ? queen.Facing : Face(path.Step));
            var target = targets.FirstOrDefault(enemy => Distance(path.At, enemy.Position) <= battle.Rules.PersuasionRadius);
            if (target != null)
                return new(path.Step, Face(target.Position - path.At));
            foreach (var step in Steps)
            {
                var next = path.At + step;
                if (routes.ContainsKey(next) && Free(battle, queen, next) && visited.Add(next))
                    queue.Enqueue((next, path.Step == Point.Zero ? step : path.Step));
            }
        }
        return new(Point.Zero, facing);
    }

    internal static Point? SafeQueenRetreat(BattleState battle, BattleUnit queen)
    {
        var enemies = battle.Units.Where(u => u.Active && !battle.Allied(u.Owner, queen.Owner)).ToArray();
        var routes = QueenEscapeRoutes(battle, queen, enemies);
        if (routes.TryGetValue(queen.Position, out var step))
            return step;
        // A queen already exposed can still step directly into safety or out of the board.
        var army = battle.Army(queen.Owner);
        foreach (var candidate in Steps)
        {
            var next = queen.Position + candidate;
            if (routes.ContainsKey(next) || battle.HomeExit(next, army))
                return candidate;
        }
        return null;
    }

    // Work backwards from actual home exits. Retreat may pass through friendly units.
    private static Dictionary<Point, Point> QueenEscapeRoutes(BattleState battle, BattleUnit queen, BattleUnit[] enemies)
    {
        var army = battle.Army(queen.Owner);
        var queue = new Queue<Point>();
        var routes = new Dictionary<Point, Point>();
        bool CanUse(Point at) => battle.WalkableFor(at, queen.Owner) &&
            !enemies.Any(enemy => ThreatensQueenCell(battle, enemy, at));
        void Add(Point at, Point step)
        {
            if (!routes.ContainsKey(at) && CanUse(at))
            {
                routes.Add(at, step);
                queue.Enqueue(at);
            }
        }

        if (battle.BoardSize == BattleState.Size && army.Side == 4)
            Add(battle.Flag, Point.Zero);
        else
        {
            var exit = BattleState.Forward((BattleFacing)army.Side);
            for (int i = 0; i < battle.BoardSize; i++)
            {
                var edge = army.Side switch
                {
                    0 => new Point(i, 0),
                    1 => new Point(battle.BoardSize - 1, i),
                    2 => new Point(i, battle.BoardSize - 1),
                    _ => new Point(0, i)
                };
                if (battle.HomeExit(edge + exit, army))
                    Add(edge, exit);
            }
        }
        while (queue.TryDequeue(out var at))
            foreach (var step in Steps)
                Add(at - step, step);
        return routes;
    }

    private static bool ThreatensQueenCell(BattleState battle, BattleUnit enemy, Point at)
    {
        int distance = Distance(enemy.Position, at);
        if (distance == 0 || enemy.Daggers > 0 && distance == 1)
            return true;
        var delta = at - enemy.Position;
        if (enemy.Rifles <= 0 || distance < 2 || delta.X != 0 && delta.Y != 0)
            return false;
        var facing = Face(delta);
        var step = BattleState.Forward(facing);
        for (int i = 1; i <= distance; i++)
            if (battle.BlocksShot(enemy.Position + new Point(i * step.X, i * step.Y), facing, i))
                return false;
        return true;
    }
}
