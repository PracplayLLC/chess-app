﻿using ChessLibrary.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChessLibrary.Serialization
{
    class PGNSerializer
    {
        private const int LineLength = 80;

        public static class WellKnownTags
        {
            public const string Event = "Event";
            public const string Site = "Site";
            public const string Date = "Date";
            public const string Round = "Round";
            public const string White = "White";
            public const string Black = "Black";
            public const string Result = "Result";
        }

        

        public async Task Serialize(Game game, PGNMetadata metadata, TextWriter writer)
        {
            var date = $"{metadata.DateTime.Year}.{metadata.DateTime.Month.ToString("00")}.{metadata.DateTime.Day.ToString("00")}";
            // TODO: Async I/O
            await writer.WriteLineAsync($"[{WellKnownTags.Event} \"{metadata.Event}\"]");
            await writer.WriteLineAsync($"[{WellKnownTags.Site} \"{metadata.Site}\"]");
            await writer.WriteLineAsync($"[{WellKnownTags.Date} \"{date}\"]");
            await writer.WriteLineAsync($"[{WellKnownTags.Round} \"{metadata.Round}\"]");
            await writer.WriteLineAsync($"[{WellKnownTags.White} \"{metadata.White}\"]");
            await writer.WriteLineAsync($"[{WellKnownTags.Black} \"{metadata.Black}\"]");
            await writer.WriteLineAsync($"[{WellKnownTags.Result} \"{metadata.Result}\"]");
            await writer.WriteLineAsync();

            // TODO: Better way to reverse
            var history = game.History.ToArray().Reverse().ToArray();

            var moves = new (Move move, BoardState board, AttackState attack)[history.Length - 1];
            for (var i = 0; i < history.Length-1; i++)
                moves[i] = (history[i + 1].PrecedingMove, history[i].Board, history[i].AttackState);

            var linePos = 0;
            for (var i = 0; i < moves.Length; i ++)
            {
                var ply = MoveParser.ToMoveString(moves[i].move, moves[i].board, moves[i].attack);
                if (ply.StartsWith('0'))
                    ply = ply.Replace('0', 'O');

                var moveNumber = string.Empty;

                if (i % 2 == 0)
                    moveNumber = ((i/2) + 1).ToString() + ". ";

                var lengthToWrite = ply.Length + moveNumber.Length;
                if (linePos + lengthToWrite + 1 > LineLength)
                {
                    await writer.WriteLineAsync();
                    linePos = 0;
                }
                else if (i != 0)
                {
                    await writer.WriteAsync(' ');
                    linePos++;
                }

                await writer.WriteAsync(moveNumber);
                await writer.WriteAsync(ply);
                linePos += lengthToWrite;
            }

            if (linePos + metadata.Result.Length + 1 > LineLength)
                await writer.WriteLineAsync();
            else
                await writer.WriteAsync(' ');

            await writer.WriteLineAsync(metadata.Result);
        }
    }
}
