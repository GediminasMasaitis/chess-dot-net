using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Init;
using ChessDotNet.MoveGeneration.SlideGeneration;
using ChessDotNet.Search2;
using ChessDotNet.Searching;

namespace ChessDotNet.Protocols
{
    public class UciProtocol : IChessProtocol
    {
        private readonly Game _game;

        public UciProtocol()
        {
            BitboardConstants.Init();
            new MagicBitboardsInitializer(new HyperbolaQuintessence(), new KnownMagicNumberProvider()).Init();

            _game = new Game();
            _game.Search.SearchInfo += OnOnSearchInfo;
        }

        private void OnOnSearchInfo(SearchInfo searchInfo)
        {
            return;
            var time = searchInfo.Time > 0 ? searchInfo.Time : 1;
            var nps = searchInfo.NodesSearched/time;
            var pv = searchInfo.PrincipalVariation.ToPositionsString();
            var score = searchInfo.MateIn.HasValue ? "mate " + searchInfo.MateIn.Value : "cp " + searchInfo.Score;
            var outStr = $"info depth {searchInfo.Depth} multipv 1 score {score} nodes {searchInfo.NodesSearched} nps {nps} time {time} pv {pv}";
            Output(outStr);
        }

        public void Input(string message)
        {
            var words = message.Trim().Split(' ');
            switch (words[0])
            {
                case "uci":
                    ConfigureUCI();
                    break;
                case "setoption":
                    SetOption(_game.Options, words);
                    break;
                case "isready":
                    Output("readyok");
                    break;
                case "ucinewgame":
                    _game.SetStartingPos();
                    break;
                case "position":
                {
                    var isStartPos = false;
                    var byFen = false;
                    var byMoves = false;
                    var fen = string.Empty;
                    var i = 1;
                    var positionParsingReady = false;
                    for (; i < words.Length && !positionParsingReady; i++)
                    {
                        switch (words[i])
                        {
                            case "startpos":
                                isStartPos = true;
                                break;
                            case "moves":
                                byMoves = true;
                                positionParsingReady = true;
                                break;
                            case "fen":
                                byFen = true;
                                i++;
                                while (true)
                                {
                                    if (i == words.Length)
                                    {
                                        positionParsingReady = true;
                                        break;
                                    }
                                    if (words[i] == "moves")
                                    {
                                        byMoves = true;
                                        positionParsingReady = true;
                                        break;
                                    }
                                    fen += words[i++] + " ";
                                }
                                break;
                        }
                    }

                    if (byFen)
                    {
                        _game.SetPositionByFEN(fen);
                    }
                    if(byMoves)
                    {
                        var moves = words.Skip(i).ToList();
                        _game.SetPositionByMoves(isStartPos && !byFen, moves);
                    }
                }
                    break;
                case "go":
                {
                    var searchParams = new SearchParameters();
                        for (var i = 0; i < words.Length; i++)
                    {
                        switch (words[i])
                        {
                            case "wtime":
                                searchParams.WhiteTime = long.Parse(words[++i]);
                                break;
                            case "btime":
                                searchParams.BlackTime = long.Parse(words[++i]);
                                break;
                            case "winc":
                                searchParams.WhiteTimeIncrement = long.Parse(words[++i]);
                                break;
                            case "binc":
                                searchParams.BlackTimeIncrement = long.Parse(words[++i]);
                                break;
                            case "infinite":
                                searchParams.Infinite = true;
                                break;
                        }

                    }
                    
                    var results = _game.SearchMove(searchParams);
                    var moveStr = results[0].ToPositionString();

                    if (results.Count == 1)
                    {
                        Output($"bestmove {moveStr}");
                    }
                    else
                    {
                        var ponderStr = results[1].ToPositionString();
                        Output($"bestmove {moveStr} ponder {ponderStr}");
                    }
                }
                    break;
                case "print":
                    Output(_game.Print());
                    break;
                case "exit":
                case "quit":
                    Exit(0);
                    break;
            }
        }

        public event Action<string> OnOutput;
        public event Action<int> OnExit;

        private void Output(string message)
        {
            OnOutput?.Invoke(message);
        }

        private void Exit(int errorCode)
        {
            OnExit?.Invoke(errorCode);
        }

        private void SetOption(SearchOptions options, string[] words)
        {
            var name = words[2];
            var value = words[4];

            var optionsType = options.GetType();
            var property = optionsType.GetProperty(name);
            var type = property.PropertyType;
            var convertedValue = Convert.ChangeType(value, type);
            property.SetValue(options, convertedValue);
        }
        
        private void ConfigureUCI()
        {
            Output("id name Chess.NET");
            Output("id author Gediminas Masaitis");
            Output(string.Empty);
            PrintOptions(_game.Options);
            Output("uciok");
        }

        private void PrintOptions(SearchOptions options)
        {
            var optionsType = options.GetType();
            var properties = optionsType.GetProperties();
            foreach (var property in properties)
            {
                var builder = new StringBuilder();

                var name = property.Name;
                builder.Append($"option name {name}");

                var value = property.GetValue(options);
                string typeStr;
                string valueStr;
                switch (value)
                {
                    case int _:
                    case uint _:
                    case long _:
                    case ulong _:
                        typeStr = "spin";
                        valueStr = value.ToString();
                        break;
                    case bool _:
                        typeStr = "check";
                        valueStr = value.ToString().ToLowerInvariant();
                        break;
                    case string valueString:
                        typeStr = "string";
                        valueStr = valueString;
                        break;
                    default:
                        typeStr = "unknown";
                        valueStr = string.Empty;
                        break;
                }
                builder.Append($" type {typeStr}");
                builder.Append($" default {valueStr}");

                var minAttribute = (MinAttribute) property.GetCustomAttribute(typeof(MinAttribute));
                if (minAttribute != null)
                {
                    builder.Append($" min {minAttribute.Min}");
                }

                var maxAttribute = (MaxAttribute)property.GetCustomAttribute(typeof(MaxAttribute));
                if (maxAttribute != null)
                {
                    builder.Append($" max {maxAttribute.Max}");
                }
                
                var result = builder.ToString();
                Output(result);
            }
        }
    }
}