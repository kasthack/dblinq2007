﻿#region MIT license
// 
// Copyright (c) 2007-2008 Jiri Moudry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
#endregion

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DbLinq.Linq.Data.Sugar.Pieces;

namespace DbLinq.Linq.Data.Sugar
{
    public class BuilderContext
    {
        // global context
        public QueryContext QueryContext { get; private set; }

        // current expression being built
        public PiecesQuery PiecesQuery { get; private set; }

        public IDictionary<Type, MetaTablePiece> MetaTables { get; private set; }
        public IDictionary<string, Piece> Parameters { get; private set; }

        public BuilderContext(QueryContext queryContext)
        {
            QueryContext = queryContext;
            PiecesQuery = new PiecesQuery();
            MetaTables = new Dictionary<Type, MetaTablePiece>();
            Parameters = new Dictionary<string, Piece>();
        }

        private BuilderContext()
        { }

        public BuilderContext Clone()
        {
            var builderContext = new BuilderContext();
            // scope independent parts
            builderContext.QueryContext = QueryContext;
            builderContext.PiecesQuery = PiecesQuery;
            builderContext.MetaTables = MetaTables;
            // scope dependent parts
            builderContext.Parameters = new Dictionary<string, Piece>(Parameters);
            return builderContext;
        }
    }
}