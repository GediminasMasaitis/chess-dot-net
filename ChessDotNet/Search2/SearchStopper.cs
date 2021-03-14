using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ChessDotNet.Searching;

namespace ChessDotNet.Search2
{
    public class SearchStopper
    {
        private readonly Stopwatch _stopwatch;

        private CancellationTokenSource _cancellationTokenSource;
        private SearchParameters _parameters;
        private long _minTime;
        private long _maxTime;

        public SearchStopper()
        {
            _stopwatch = new Stopwatch();
        }

        public void NewSearch(SearchParameters parameters, bool whiteToMove, CancellationToken externalToken)
        {
            _stopwatch.Restart();
            _parameters = parameters;

            var time = whiteToMove ? parameters.WhiteTime : parameters.BlackTime;
            var increment = whiteToMove ? parameters.WhiteTimeIncrement : parameters.BlackTimeIncrement;

            // Need to adjust this
            _minTime = parameters.Infinite ? long.MaxValue : time / 60 + increment / 3;
            _maxTime = parameters.Infinite ? long.MaxValue : time / 20 + increment;
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        }

        public bool ShouldStopOnDepthIncrease(int depthReached)
        {
            if (_parameters.MaxDepth.HasValue && depthReached >= _parameters.MaxDepth.Value)
            {
                _cancellationTokenSource.Cancel();
                return true;
            }

            var elapsed = GetSearchedTime();
            if (elapsed >= _minTime)
            {
                _cancellationTokenSource.Cancel();
                return true;
            }

            var cancellationRequested = _cancellationTokenSource.IsCancellationRequested;
            return cancellationRequested;
        }

        public bool ShouldStop()
        {
            var elapsed = GetSearchedTime();
            if (elapsed >= _maxTime)
            {
                _cancellationTokenSource.Cancel();
                return true;
            }

            return _cancellationTokenSource.IsCancellationRequested;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsStopped()
        {
            return _cancellationTokenSource.IsCancellationRequested;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSearchedTime()
        {
            return _stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}