using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation
{
    public interface IEvaluationService
    {
        int Evaluate(Board board, Span<ulong> pins);
    }
}