using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Searching;

namespace ChessDotNet.Protocols
{
    public class UCIProtocol : IChessProtocol
    {
        private Game Game { get; set; }

        public UCIProtocol(IInterruptor interruptor)
        {
            Game = new Game(interruptor);
            Game.Search.OnSearchInfo += OnOnSearchInfo;
        }

        private void OnOnSearchInfo(SearchInfo searchInfo)
        {
            var time = searchInfo.Time > 0 ? searchInfo.Time : 1;
            var nps = searchInfo.NodesSearched/time;
            var pv = searchInfo.PrincipalVariation.Select(x => x.Move.ToPositionString()).Aggregate((x, n) => x + " " + n);
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
                    break;
                case "isready":
                    Output("readyok");
                    break;
                case "ucinewgame":
                    Game.SetStartingPos();
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
                        Game.SetPositionByFEN(fen);
                    }
                    if(byMoves)
                    {
                        var moves = words.Skip(i).ToList();
                        Game.SetPositionByMoves(isStartPos && !byFen, moves);
                    }
                }
                    break;
                case "go":
                {
                    var searchParams = new SearchParams();
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

                    var results = Game.SearchMove(searchParams);
                    var moveStr = results[0].Move.ToPositionString();

                    if (results[1] == default(TTEntry))
                    {
                        Output($"bestmove {moveStr}");
                    }
                    else
                    {
                        var ponderStr = results[1].Move.ToPositionString();
                        Output($"bestmove {moveStr} ponder {ponderStr}");
                    }
                }
                    break;
                case "print":
                    Output(Game.Print());
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
        
        private void ConfigureUCI()
        {
            Output("id name Chess.NET");
            Output("id author Gediminas Masaitis");
            Output("uciok");
        }


    }
}