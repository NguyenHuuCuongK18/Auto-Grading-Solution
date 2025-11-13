using ConsoleTableExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleAppLibrary
{
    public class ConsoleTable
    {
        public static void DrawTable(List<List<object>> tableData)
        {
            if (tableData != null)
            {
                ConsoleTableBuilder
                .From(tableData)
                .ExportAndWriteLine()
                ;
            }

        }
        public static void DrawTable(List<List<object>> tableData, string title)
        {
            if (tableData != null)
            {
                ConsoleTableBuilder
                .From(tableData)
                .WithTitle(title)
                .WithCharMapDefinition(new Dictionary<CharMapPositions, char> {
        {CharMapPositions.BottomLeft, '=' },
        {CharMapPositions.BottomCenter, '=' },
        {CharMapPositions.BottomRight, '=' },
        {CharMapPositions.BorderTop, '=' },
        {CharMapPositions.BorderBottom, '=' },
        {CharMapPositions.BorderLeft, '|' },
        {CharMapPositions.BorderRight, '|' },
        {CharMapPositions.DividerY, '|' },
    })
    .WithHeaderCharMapDefinition(new Dictionary<HeaderCharMapPositions, char> {
        {HeaderCharMapPositions.TopLeft, '=' },
        {HeaderCharMapPositions.TopCenter, '=' },
        {HeaderCharMapPositions.TopRight, '=' },
        {HeaderCharMapPositions.BottomLeft, '|' },
        {HeaderCharMapPositions.BottomCenter, '-' },
        {HeaderCharMapPositions.BottomRight, '|' },
        {HeaderCharMapPositions.Divider, '|' },
        {HeaderCharMapPositions.BorderTop, '=' },
        {HeaderCharMapPositions.BorderBottom, '-' },
        {HeaderCharMapPositions.BorderLeft, '|' },
        {HeaderCharMapPositions.BorderRight, '|' },
    })
                .ExportAndWriteLine()
                ;
            }

        }
    }
}
