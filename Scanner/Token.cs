using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scanner
{

    public enum TokenType
    {
        Keyword,
        Identifier,
        Number,
        StringLiteral,
        Operator,
        Delimiter,
        Comment,
        Whitespace,
        Unknown,
        EOF
    }
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"{Type}('{Value}') at {Line}:{Column}";
    }

}
