using System.Text;
using System.Collections.Generic;
using Scanner;
using System;

namespace Scanner
{
    public class Scanner
    {
        private readonly string _source;
        private int _index = 0;
        private int _line = 0;
        private int _column = 1;
        private readonly List<Token> _tokens = new();

        public Scanner(string source)
        {
            _source = source;
        }

        public List<Token> Scan()
        {
            while (!IsAtEnd())
            {
                char current = Peek();

                // check for white spaces
                if (char.IsWhiteSpace(current))
                {
                    HandleWhitespace();
                    continue;
                }

                // check the line comments and multi-line comments
                if (current == '/')
                {
                    if (Match("//"))
                    {
                        _tokens.Add(HandleLineComment());
                        continue;
                    }
                    else if (Match("/*"))
                    {
                        _tokens.Add(HandleMultiLineComment());
                        continue;
                    }
                }

                // check for the IDs and keywords
                if (char.IsLetter(current) || current == '_')
                {
                    _tokens.Add(HandleIdentifierOrKeyword());
                    continue;
                }

                // check for the numbers
                if (char.IsDigit(current) || (current == '.' && char.IsDigit(PeekAhead())))
                {
                    _tokens.Add(HandleNumber());
                    continue;
                }

                // check for strings
                if (current == '"')
                {
                    _tokens.Add(HandleString());
                    continue;
                }

                // check for delimiters
                if (CDefinitions.Delimiters.Contains(current))
                {
                    _tokens.Add(HandleDelimiter());
                    continue;
                }

                // check for operators
                if (CDefinitions.Operators.Contains(current.ToString()))
                {
                    _tokens.Add(HandleOperator());
                    continue;
                }

                // add the unknown symbol
                _tokens.Add(new Token(TokenType.Unknown, current.ToString(), _line, _column));
                Advance();
            }

            // add the end of code token
            _tokens.Add(new Token(TokenType.EOF, "", _line, _column));
            return _tokens;
        }

        private void HandleWhitespace()
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 0;
            }
            Advance();
        }
        private Token HandleLineComment()
        {
            int start = _index
                ,col=_column;

            Advance();
            Advance();

            while (!IsAtEnd() && Peek() != '\n')
                Advance();

            string comment = _source.Substring(start, _index - start);

            return new Token(TokenType.Comment, comment.TrimEnd('\r','\n'), _line, col);
        }

        private Token HandleMultiLineComment()
        {
            int start = _index
                ,col=_column;
            Advance();
            Advance();

            while (!IsAtEnd())
            {
                if (Peek() == '*' && PeekAhead() == '/')
                {
                    Advance();
                    Advance();
                    break;
                }
                if (Peek() == '\n')
                {
                    _line++;
                    _column = 0;
                }
                Advance();
            }
            string comment = _source.Substring(start, _index - start);
            return new Token(TokenType.Comment, comment, _line, col);

        }

        private Token HandleIdentifierOrKeyword()
        {
            int start = _index,
                startColumn = _column;
            while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
                Advance();

            string text = _source.Substring(start, _index - start);
            var type = CDefinitions.Keywords.Contains(text) ? TokenType.Keyword : TokenType.Identifier;
            return new Token(type, text, _line, startColumn);
        }

        private Token HandleNumber()
        {
            int start = _index,
                startColumn = _column;
            bool notDecimal = true;

            // consume integer and fractional part
            while (!IsAtEnd() && (char.IsDigit(Peek()) || (notDecimal && Peek() == '.')))
            {
                if (Peek() == '.')
                    notDecimal = false;

                Advance();
            }

            // check for exponent part (e or E)
            if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
            {
                Advance(); // consume e/E

                // optional sign after exponent
                if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
                    Advance();

                // there must be at least one digit after e/E
                if (!IsAtEnd() && char.IsDigit(Peek()))
                {
                    while (!IsAtEnd() && char.IsDigit(Peek()))
                        Advance();
                }
                else
                {
                    // malformed exponent like "1e" or "2E-"
                    // just step back one position to ignore the invalid e/E
                    _index--;
                    _column--;
                }
            }

            string number = _source.Substring(start, _index - start);
            return new Token(TokenType.Number, number, _line, startColumn);
        }


        private Token HandleString()
        {
            Advance(); // skip opening "
            int start = _index,
                startColumn = _column;
            while (!IsAtEnd() && Peek() != '"')
                Advance();

            string str = _source.Substring(start, _index - start);
            Advance(); // skip closing "
            return new Token(TokenType.StringLiteral, str, _line, startColumn);
        }

        private Token HandleDelimiter()
        {
            int start = _index,
                startColumn = _column;
            Advance();
            return new Token(TokenType.Delimiter, _source[start].ToString(), _line, startColumn);
        }

        private Token HandleOperator()
        {
            int start = _index,
                startColumn = _column;
            if (CDefinitions.Operators.Contains($"{Peek()}{PeekAhead()}"))
            {
                Advance();
                Advance();
                string op = _source[start.._index];

                return new Token(TokenType.Operator, op, _line, startColumn);
            }
            Advance();
            string singleOp = _source[start].ToString();
            return new Token(TokenType.Operator, singleOp, _line, startColumn);
        }

        private bool IsAtEnd() => _index >= _source.Length;
        private char Peek() => _source[_index];
        private char PeekAhead() => _index + 1 < _source.Length ? _source[_index + 1] : '\0';
        private void Advance() { _index++; _column++; }

        private bool Match(string s)
        {
            if (_index + s.Length > _source.Length) return false;
            return _source.Substring(_index, s.Length) == s;
        }
    }
}