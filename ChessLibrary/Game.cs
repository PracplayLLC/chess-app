﻿using ChessLibrary.Models;
using System;
using System.Collections.Generic;

namespace ChessLibrary
{
    public class Game
    {
        private Stack<BoardState> History { get; } = new Stack<BoardState>();
        private BoardState BoardState { get; set; }
        public AttackState AttackState { get; private set; }
        private ulong CurrentTurn { get; set; }

        public Game() : this(BoardState.DefaultPositions, PieceColor.White)
        { }

        internal Game(BoardState state) : this(state, PieceColor.White)
        { }

        internal Game(BoardState state, PieceColor turn)
        {
            BoardState = state;
            CurrentTurn = turn == PieceColor.White ? BoardState.WhitePieces : BoardState.BlackPieces;
            AttackState = AttackState.None;
        }

        public PieceColor GetTurn()
        {
            return (CurrentTurn & BoardState.WhitePieces) != 0
                ? PieceColor.White
                : PieceColor.Black;
        }

        public SquareContents GetSquareContents(char file, int rank)
        {
            var result = (SquareContents)0;

            if (BitTranslator.IsValidSquare(file, rank))
            {
                ulong startSquare = BitTranslator.TranslateToBit(file, rank);

                if ((startSquare & BoardState.WhitePieces) != 0)
                    result |= SquareContents.White;
                else if ((startSquare & BoardState.BlackPieces) != 0)
                    result |= SquareContents.Black;

                if ((startSquare & BoardState.Kings) != 0)
                    result |= SquareContents.King;
                else if ((startSquare & BoardState.Queens) != 0)
                    result |= SquareContents.Queen;
                else if ((startSquare & BoardState.Rooks) != 0)
                    result |= SquareContents.Rook;
                else if ((startSquare & BoardState.Bishops) != 0)
                    result |= SquareContents.Bishop;
                else if ((startSquare & BoardState.Knights) != 0)
                    result |= SquareContents.Knight;
                else if ((startSquare & BoardState.Pawns) != 0)
                    result |= SquareContents.Pawn;
            }

            return result;
        }

        public ErrorConditions Move(string input)
        {
            if (MoveParser.TryParseMove(input, BoardState, CurrentTurn, out Move move))
                return Move(move);

            return ErrorConditions.InvalidInput;
        }

        public ErrorConditions Move(Move move)
        {
            if (!BitTranslator.IsValidSquare(move.StartFile, move.StartRank))
                return ErrorConditions.InvalidSquare;
            if (!BitTranslator.IsValidSquare(move.EndFile, move.EndRank))
                return ErrorConditions.InvalidSquare;

            ulong startSquare = BitTranslator.TranslateToBit(move.StartFile, move.StartRank);
            if ((CurrentTurn & startSquare) == 0)
                return ErrorConditions.MustMoveOwnPiece; // Can't move if not your turn

            ulong endSquare = BitTranslator.TranslateToBit(move.EndFile, move.EndRank);
            if ((CurrentTurn & endSquare) != 0)
                return ErrorConditions.CantTakeOwnPiece; // Can't end move on own piece

            ulong allMoves = MoveGenerator.GenerateMovesForPiece(BoardState, startSquare);
            if ((endSquare & allMoves) == 0)
                return ErrorConditions.InvalidMovement; // End square is not a valid move

            // The move is good, so update state
            // Update current state
            UpdateState(move, startSquare, endSquare);

            return ErrorConditions.None;
        }

        private void UpdateState(Move move, ulong startSquare, ulong endSquare)
        {
            // All state detections
            // ✔ Account for piece promotions
            // ✔ Detect checks
            // ✔ Detect checkmate
            // ✔ Detect stalemate
            // ✔ Detect draw by repetition
            // ✔ Detect draw by inactivity (50 moves without a capture)

            // TODO: Ensure we clear old state on en passant
            //
            var isCapture = (BoardState.AllPieces & endSquare) != 0;
            var isPawn = (BoardState.Pawns & startSquare) != 0;

            BoardState = BoardState.MovePiece(startSquare, endSquare);
            if (move.PromotedPiece != SquareContents.Empty)
                BoardState = BoardState.SetPiece(endSquare, move.PromotedPiece);

            if (isCapture || isPawn)
                History.Clear();
            History.Push(BoardState);

            var ownPieces = (endSquare & BoardState.WhitePieces) != 0
                ? BoardState.WhitePieces : BoardState.BlackPieces;
            var opponentPieces = BoardState.AllPieces & ~ownPieces;

            var ownMovements = MoveGenerator.GenerateStandardMoves(BoardState, ownPieces, 0);
            var opponentMovements = MoveGenerator.GenerateMoves(BoardState, opponentPieces, ownMovements);

            var opponentKingUnderAttack = (opponentPieces & BoardState.Kings & ownMovements) != 0;
            var opponentCanMove = opponentMovements != 0;

            if (opponentKingUnderAttack)
                AttackState = opponentCanMove ? AttackState.Check : AttackState.Checkmate;
            else if (!opponentCanMove)
                AttackState = AttackState.Stalemate;
            else if (History.Count == Constants.MoveLimits.InactivityLimit)
                AttackState = AttackState.DrawByInactivity;
            else if (CountHistoricalOccurrences(BoardState) >= Constants.MoveLimits.RepetitionLimit)
                AttackState = AttackState.DrawByRepetition;
            else
                AttackState = AttackState.None;

            CurrentTurn = opponentPieces;
        }

        private int CountHistoricalOccurrences(BoardState newState)
        {
            var count = 0;

            foreach (var state in History)
                if (state.Equals(newState))
                    count++;

            return count;
        }

        internal ErrorConditions Move(char startFile, int startRank, char endFile, int endRank)
        {
            return Move(new Move()
            {
                StartFile = startFile,
                StartRank = startRank,
                EndFile = endFile,
                EndRank = endRank
            });
        }

        public List<Square> GetValidMoves(char file, int rank)
        {
            if (!BitTranslator.IsValidSquare(file, rank))
                throw new InvalidOperationException();

            var square = BitTranslator.TranslateToBit(file, rank);
            ulong allMoves = MoveGenerator.GenerateMovesForPiece(BoardState, square);

            return BitTranslator.TranslateToSquares(allMoves);
        }

        public Move ParseMove(string input)
        {
            if (MoveParser.TryParseMove(input, BoardState, CurrentTurn, out Move move))
                return move;

            throw new FormatException($"Could not parse move '{input}' for current board state");
        }

        public static Square ParseSquare(string input)
        {
            return MoveParser.ParseSquare(input);
        }
    }
}
