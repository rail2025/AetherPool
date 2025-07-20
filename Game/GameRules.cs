using AetherPool.Game.GameObjects;
using System.Collections.Generic;
using System.Linq;

namespace AetherPool.Game
{
    public static class GameRules
    {
        public static List<PoolBall> GetLegalTargets(GameSession session, bool beforeShot = false)
        {
            var ballsOnTable = session.Balls.Where(b => !b.IsSunk).ToList();

            // If checking legality for a shot that just happened, we need to consider balls pocketed during that shot as having been on the table.
            if (beforeShot)
            {
                var pocketedThisTurn = session.Balls.Where(b => b.IsSunk && !session.LastTurnSunkBalls.Contains(b)).ToList();
                ballsOnTable.AddRange(pocketedThisTurn);
            }

            if (session.CurrentMode == GameMode.NineBall)
            {
                PoolBall? lowestBall = ballsOnTable
                    .Where(b => b.Number > 0)
                    .OrderBy(b => b.Number)
                    .FirstOrDefault();

                return lowestBall != null ? new List<PoolBall> { lowestBall } : new List<PoolBall>();
            }

            if (session.Player1Assignment == PlayerAssignment.Unassigned)
                return ballsOnTable.Where(b => b.Number > 0 && b.Number != 8).ToList();

            var assignment = (session.CurrentTurn == Player.Human) ? session.Player1Assignment : session.Player2Assignment;
            var targets = session.CurrentMode == GameMode.EightBall
                ? ballsOnTable.Where(b => assignment == PlayerAssignment.Solids
                                    ? (b.Number >= 1 && b.Number <= 7)
                                    : (b.Number >= 9 && b.Number <= 15)).ToList()
                : new List<PoolBall>();

            if (!targets.Any() && session.CurrentMode == GameMode.EightBall)
            {
                var eight = ballsOnTable.FirstOrDefault(b => b.Number == 8);
                if (eight != null) targets.Add(eight);
            }

            return targets;
        }

        public static (TurnOutcome outcome, string? reason) ProcessTurnEnd(GameSession session)
        {
            var pocketed = session.Balls.Where(b => b.IsSunk && !session.LastTurnSunkBalls.Contains(b)).ToList();
            var firstHit = session.FirstBallHitThisTurn;

            // *** THE FIX: Get the legal targets as they were BEFORE the shot was taken ***
            var legalTargetsBeforeShot = GetLegalTargets(session, true);

            // --- FOUL CHECKS ---
            if (pocketed.Any(b => b.Number == 0)) return (TurnOutcome.FoulSwitchTurn, "Cue ball was pocketed.");
            if (firstHit == null) return (TurnOutcome.FoulSwitchTurn, "No ball was hit.");
            if (!legalTargetsBeforeShot.Any()) return (TurnOutcome.KeepTurn, null); // Not a foul if no legal targets were on the table
            if (!legalTargetsBeforeShot.Contains(firstHit)) return (TurnOutcome.FoulSwitchTurn, $"Illegal hit: Must hit the {legalTargetsBeforeShot.First().Number}-ball first.");
            if (pocketed.Count == 0 && !session.CollisionEvents.Any(e => e.Type == CollisionType.Cushion)) return (TurnOutcome.FoulSwitchTurn, "No ball hit a rail after contact.");

            // --- WIN/LOSS AND ASSIGNMENT ---
            if (session.CurrentMode == GameMode.NineBall)
            {
                if (pocketed.Any(b => b.Number == 9)) return (TurnOutcome.PlayerWins, null);
            }
            else // 8-Ball
            {
                if (pocketed.Any(b => b.Number == 8))
                {
                    // Check if all of the player's balls were cleared BEFORE this shot
                    var ballsBeforeShot = new List<PoolBall>(session.Balls);
                    pocketed.ForEach(p => ballsBeforeShot.First(b => b.Number == p.Number).IsSunk = false);

                    if (AllBallsCleared(session, ballsBeforeShot)) return (TurnOutcome.PlayerWins, null);
                    return (TurnOutcome.PlayerLoses, "Pocketed the 8-ball too early.");
                }
                if (session.Player1Assignment == PlayerAssignment.Unassigned)
                {
                    AssignBallGroup(session, pocketed);
                }
            }

            // --- TURN CONTINUATION ---
            if (pocketed.Any())
            {
                return (TurnOutcome.KeepTurn, null);
            }

            return (TurnOutcome.SwitchTurn, null);
        }

        private static bool AllBallsCleared(GameSession session, List<PoolBall>? ballsToCheck = null)
        {
            var assign = session.CurrentTurn == Player.Human ? session.Player1Assignment : session.Player2Assignment;
            var ballList = ballsToCheck ?? session.Balls;

            return assign switch
            {
                PlayerAssignment.Solids => !ballList.Any(b => !b.IsSunk && b.Number >= 1 && b.Number <= 7),
                PlayerAssignment.Stripes => !ballList.Any(b => !b.IsSunk && b.Number >= 9 && b.Number <= 15),
                _ => false
            };
        }

        private static void AssignBallGroup(GameSession session, List<PoolBall> pocketed)
        {
            int solids = pocketed.Count(b => b.Number is >= 1 and <= 7);
            int stripes = pocketed.Count(b => b.Number is >= 9 and <= 15);

            if (solids > 0 && stripes == 0) Assign(session, PlayerAssignment.Solids);
            else if (stripes > 0 && solids == 0) Assign(session, PlayerAssignment.Stripes);
        }

        private static void Assign(GameSession session, PlayerAssignment assignment)
        {
            var opposite = assignment == PlayerAssignment.Solids ? PlayerAssignment.Stripes : PlayerAssignment.Solids;
            if (session.CurrentTurn == Player.Human)
            {
                session.Player1Assignment = assignment;
                session.Player2Assignment = opposite;
            }
            else
            {
                session.Player2Assignment = assignment;
                session.Player1Assignment = opposite;
            }
        }
    }
}
