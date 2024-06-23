﻿using Model.Core;
using Model.Core.Changes;
using Model.Core.Highlighting;

namespace Model.Kakuros;

public interface IKakuroSolverData : INumericSolvingState
{
    IReadOnlyKakuro Kakuro { get; }
    IChangeBuffer<IUpdatableNumericSolvingState, INumericSolvingStateHighlighter> ChangeBuffer { get; }
}