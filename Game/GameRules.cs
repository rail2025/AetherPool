using AetherPool.Game.GameObjects;
using System.Collections.Generic;
using System.Linq;

namespace AetherPool.Game
{
    public enum PlayerAssignment { Unassigned, Solids, Stripes }

    public enum TurnOutcome { KeepTurn, SwitchTurn, FoulSwitchTurn, PlayerWins, PlayerLoses }

    public static class GameRules
    {
        public static List<PoolBall> GetLegalTargets(GameSession session)
        {
            var balls = session.Balls.Where(b => !b.IsSunk).ToList();

            if (session.CurrentMode == GameMode.NineBall)
            {
                var lowestBall = balls.Where(b => b.Number > 0).OrderBy(b => b.Number).FirstOrDefault();
                return lowestBall != null ? new List<PoolBall> { lowestBall } : new List<PoolBall>();
            }

            if (session.Player1Assignment == PlayerAssignment.Unassigned)
            {
                return balls.Where(b => b.Number is > 0 and < 8 or > 8).ToList();
            }

            var playerTargets = new List<PoolBall>();
            var currentPlayerAssignment = session.CurrentTurn == Player.Human ? session.Player1Assignment : session.Player2Assignment;

            if (currentPlayerAssignment == PlayerAssignment.Solids)
            {
                playerTargets.AddRange(balls.Where(b => b.Number is >= 1 and <= 7));
            }
            else if (currentPlayerAssignment == PlayerAssignment.Stripes)
            {
                playerTargets.AddRange(balls.Where(b => b.Number is >= 9 and <= 15));
            }

            if (!playerTargets.Any())
            {
                var eightBall = balls.FirstOrDefault(b => b.Number == 8);
                if (eightBall != null) playerTargets.Add(eightBall);
            }

            return playerTargets;
        }

        public static TurnOutcome ProcessTurnEnd(GameSession session)
        {
            var pocketedThisTurn = session.Balls.Where(b => b.IsSunk && !session.LastTurnSunkBalls.Contains(b)).ToList();

            if (pocketedThisTurn.Any(b => b.Number == 0)) return TurnOutcome.FoulSwitchTurn;
            if (session.FirstBallHitThisTurn == null) return TurnOutcome.FoulSwitchTurn;

            var legalTargets = GetLegalTargets(session);
            if (!legalTargets.Any(b => b.Number == session.FirstBallHitThisTurn.Number))
            {
                return TurnOutcome.FoulSwitchTurn;
            }

            if (session.CurrentMode == GameMode.EightBall)
            {
                if (pocketedThisTurn.Any(b => b.Number == 8))
                {
                    return legalTargets.Any(b => b.Number == 8) ? TurnOutcome.PlayerWins : TurnOutcome.PlayerLoses;
                }
            }
            else if (session.CurrentMode == GameMode.NineBall)
            {
                if (pocketedThisTurn.Any(b => b.Number == 9)) return TurnOutcome.PlayerWins;
            }

            bool playerPocketedLegalBall = pocketedThisTurn.Any(p => legalTargets.Any(lt => lt.Number == p.Number));

            // In 9-Ball, you also keep your turn if you pocket ANY ball legally.
            if (session.CurrentMode == GameMode.NineBall && pocketedThisTurn.Any())
            {
                playerPocketedLegalBall = true;
            }

            if (playerPocketedLegalBall)
            {
                if (session.CurrentMode == GameMode.EightBall && session.Player1Assignment == PlayerAssignment.Unassigned)
                {
                    var firstPocketed = pocketedThisTurn.FirstOrDefault(p => p.Number != 0 && p.Number != 8);
                    if (firstPocketed != null)
                    {
                        var assignment = (firstPocketed.Number < 8) ? PlayerAssignment.Solids : PlayerAssignment.Stripes;
                        if (session.CurrentTurn == Player.Human)
                        {
                            session.Player1Assignment = assignment;
                            session.Player2Assignment = (assignment == PlayerAssignment.Solids) ? PlayerAssignment.Stripes : PlayerAssignment.Solids;
                        }
                        else
                        {
                            session.Player2Assignment = assignment;
                            session.Player1Assignment = (assignment == PlayerAssignment.Solids) ? PlayerAssignment.Stripes : PlayerAssignment.Solids;
                        }
                    }
                }
                return TurnOutcome.KeepTurn;
            }

            return TurnOutcome.SwitchTurn;
        }
    }
}
