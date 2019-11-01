// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orang.CommandLine
{
    internal class GroupWriter
    {
        private static readonly ImmutableArray<ConsoleColors> _allColors = ImmutableArray.Create(
            Colors.Match_Group1,
            Colors.Match_Group2,
            Colors.Match_Group3,
            Colors.Match_Group4);

        private Stack<GroupItem> _pendingGroups;

        public GroupWriter(ValueWriter valueWriter, StringWriter writer = null)
        {
            ValueWriter = valueWriter;
            Writer = writer;
        }

        public ValueWriter ValueWriter { get; }

        public StringWriter Writer { get; }

        internal Dictionary<int, ConsoleColors> IndexesToColors { get; private set; }

        public void WriteMatch(string input, MatchItem matchItem, int groupNumber, OutputSymbols symbols)
        {
            if (_pendingGroups == null)
                _pendingGroups = new Stack<GroupItem>();

            if (IndexesToColors == null)
            {
                IndexesToColors = new Dictionary<int, ConsoleColors>();
            }
            else
            {
                IndexesToColors.Clear();
            }

            int colorIndex = 0;

            int lastPos = matchItem.Index;

            Write(OutputSymbols.Default.OpenBoundary, Colors.MatchBoundary);

            foreach (GroupItem groupItem in matchItem.GroupItems
                .Where(f => f.Number != 0
                    && ((groupNumber >= 0) ? groupNumber == f.Number : true))
                .OrderBy(f => f.Index)
                .ThenBy(f => f.Number))
            {
                ClosePendingGroups(groupItem.Index);

                ValueWriter.Write(input, lastPos, groupItem.Index - lastPos, symbols);

                ConsoleColors groupColors = GetGroupColors(groupItem);

                Write("(", groupColors);
                Write(groupItem.Name, groupColors);
                Write(":", groupColors);

                _pendingGroups.Push(groupItem);
                lastPos = groupItem.Index;
            }

            ClosePendingGroups(matchItem.EndIndex);

            ValueWriter.Write(input, lastPos, matchItem.EndIndex - lastPos, symbols);
            Write(OutputSymbols.Default.CloseBoundary, Colors.MatchBoundary);

            Debug.Assert(_pendingGroups.Count == 0, _pendingGroups.Count.ToString());

            void ClosePendingGroups(int index)
            {
                while (_pendingGroups.Count > 0
                    && _pendingGroups.Peek().EndIndex <= index)
                {
                    GroupItem groupItem = _pendingGroups.Pop();
                    ValueWriter.Write(input, lastPos, groupItem.EndIndex - lastPos, symbols);
                    Write(")", GetGroupColors(groupItem));
                    lastPos = groupItem.EndIndex;
                }
            }

            ConsoleColors GetGroupColors(GroupItem groupItem)
            {
                if (!IndexesToColors.TryGetValue(groupItem.Number, out ConsoleColors groupColors))
                {
                    groupColors = _allColors[colorIndex % 4];
                    IndexesToColors[groupItem.Number] = groupColors;
                    colorIndex++;
                }

                return groupColors;
            }
        }

        private void Write(string value, in ConsoleColors colors)
        {
            Logger.Write(value, colors);
            Writer?.Write(value);
        }
    }
}
