using System.Text;
using System.Collections.Generic;
using Scanner;

namespace Scanner
{
    public class Scanner
    {
        private readonly string _src;
        private readonly bool _keepComments;
        private int _idx = 0;
        private int _line = 1;
        private int _col = 1;
        private readonly List<Token> _tokens = new();

        // Operators sorted longest-first to ensure maximal munch
        private readonly string[] _operatorsSorted;

        public Scanner(string source, bool keepComments = false)
        {
            _src = source ?? string.Empty;
            _keepComments = keepComments;
            _operatorsSorted = CDefinitions.Operators.OrderByDescending(s => s.Length).ToArray();
        }

        public IReadOnlyList<Token> Scan()
        {
            while (!IsAtEnd())
            {
                char current = Peek();

                if (char.IsWhiteSpace(current))
                {
                    HandleWhitespace();
                    continue;
                }

                // Comments: // or /*
                if (current == '/')
                {
                    if (MatchAhead("//"))
                    {
                        HandleLineComment();
                        continue;
                    }
                    else if (MatchAhead("/*"))
                    {
                        HandleBlockComment();
                        continue;
                    }
                }

                // Identifiers or keywords
                if (char.IsLetter(current) || current == '_')
                {
                    _tokens.Add(ReadIdentifierOrKeyword());
                    continue;
                }

                // Number starting with digit or a dot followed by digit (e.g. .5)
                if (char.IsDigit(current) || (current == '.' && NextIsDigit()))
                {
                    _tokens.Add(ReadNumber());
                    continue;
                }

                // String literal
                if (current == '"')
                {
                    _tokens.Add(ReadStringLiteral());
                    continue;
                }

                // Char literal
                if (current == '\'')
                {
                    _tokens.Add(ReadCharLiteral());
                    continue;
                }

                // Delimiters
                if (CDefinitions.Delimiters.Contains(current))
                {
                    AddToken(TokenType.Delimiter, current.ToString());
                    Advance();
                    continue;
                }

                // Operators (longest-match)
                string op = TryMatchOperator();
                if (op != null)
                {
                    AddToken(TokenType.Operator, op);
                    continue;
                }

                // Unrecognized single character
                AddToken(TokenType.Unknown, current.ToString());
                Advance();
            }

            _tokens.Add(new Token(TokenType.EOF, string.Empty, _line, _col));
            return _tokens;
        }

        private void HandleWhitespace()
        {
            int startLine = _line, startCol = _col;
            var sb = new StringBuilder();
            while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
            {
                char c = Peek();
                sb.Append(c);
                if (c == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                _idx++;
            }
        }

        private void HandleLineComment()
        {
            int startLine = _line, startCol = _col;
            // consume the "//"
            Advance(); // '/'
            Advance(); // '/'
            var sb = new StringBuilder();
            while (!IsAtEnd() && Peek() != '\n')
            {
                sb.Append(Peek());
                Advance();
            }
            // newline consumed by whitespace handler at next loop iteration
            if (_keepComments)
                _tokens.Add(new Token(TokenType.Comment, "//" + sb.ToString(), startLine, startCol));
        }

        private void HandleBlockComment()
        {
            int startLine = _line, startCol = _col;
            Advance(); // '/'
            Advance(); // '*'
            var sb = new StringBuilder("/*");
            while (!IsAtEnd())
            {
                if (MatchAhead("*/"))
                {
                    // include closing
                    sb.Append(Peek()); Advance(); // '*'
                    sb.Append(Peek()); Advance(); // '/'
                    break;
                }
                char c = Peek();
                sb.Append(c);
                if (c == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                _idx++;
            }
            if (_keepComments)
                _tokens.Add(new Token(TokenType.Comment, sb.ToString(), startLine, startCol));
        }

        private Token ReadIdentifierOrKeyword()
        {
            int startIdx = _idx;
            int startLine = _line, startCol = _col;
            while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
                Advance();

            string text = _src.Substring(startIdx, _idx - startIdx);
            TokenType type = CDefinitions.Keywords.Contains(text) ? TokenType.Keyword : TokenType.Identifier;
            return new Token(type, text, startLine, startCol);
        }

        private Token ReadNumber()
        {
            // supports:
            // 123, 123.456, 123., .456, 1e10, 1.2E-3, .5e+2
            int startIdx = _idx;
            int startLine = _line, startCol = _col;

            bool hasDigitsBeforeDot = false;
            // integer part
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                hasDigitsBeforeDot = true;
                Advance();
            }

            // fractional
            if (!IsAtEnd() && Peek() == '.')
            {
                // check it's a decimal point not an operator (like '..' doesn't exist in C but be safe)
                if (NextIsDigit() || hasDigitsBeforeDot)
                {
                    Advance(); // consume '.'
                    while (!IsAtEnd() && char.IsDigit(Peek()))
                        Advance();
                }
                else
                {
                    // dot not followed by digit and no digits before -> treat as operator/unknown; but this branch won't often run because we guard with NextIsDigit earlier
                }
            }

            // exponent part
            if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
            {
                int saveIdx = _idx;
                int saveLine = _line, saveCol = _col;

                Advance(); // e/E
                if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
                    Advance();
                bool hasExpDigits = false;
                while (!IsAtEnd() && char.IsDigit(Peek()))
                {
                    hasExpDigits = true;
                    Advance();
                }

                if (!hasExpDigits)
                {
                    // Rollback exponent if malformed (treat 'e' as part of identifier/unknown)
                    _idx = saveIdx;
                    _line = saveLine;
                    _col = saveCol;
                }
            }

            string num = _src.Substring(startIdx, _idx - startIdx);
            return new Token(TokenType.Number, num, startLine, startCol);
        }

        private Token ReadStringLiteral()
        {
            int startLine = _line, startCol = _col;
            var sb = new StringBuilder();
            Advance(); // skip opening "
            bool closed = false;
            while (!IsAtEnd())
            {
                char c = Peek();
                if (c == '\\')
                {
                    // escape sequence: include backslash and next char if present
                    sb.Append(c);
                    Advance();
                    if (!IsAtEnd())
                    {
                        sb.Append(Peek());
                        Advance();
                    }
                    continue;
                }
                if (c == '"')
                {
                    Advance(); // consume closing "
                    closed = true;
                    break;
                }
                if (c == '\n')
                {
                    // string literals in C cannot contain newlines unescaped; we'll still handle it gracefully
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                sb.Append(c);
                _idx++;
            }

            string content = sb.ToString();
            return new Token(TokenType.StringLiteral, content, startLine, startCol);
        }

        private Token ReadCharLiteral()
        {
            int startLine = _line, startCol = _col;
            var sb = new StringBuilder();
            Advance(); // skip opening '
            while (!IsAtEnd())
            {
                char c = Peek();
                if (c == '\\')
                {
                    sb.Append(c);
                    Advance();
                    if (!IsAtEnd())
                    {
                        sb.Append(Peek());
                        Advance();
                    }
                    continue;
                }
                if (c == '\'')
                {
                    Advance(); // closing '
                    break;
                }
                if (c == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }
                sb.Append(c);
                _idx++;
            }
            return new Token(TokenType.CharLiteral, sb.ToString(), startLine, startCol);
        }

        private string TryMatchOperator()
        {
            foreach (var op in _operatorsSorted)
            {
                if (MatchAhead(op))
                {
                    int startLine = _line, startCol = _col;
                    for (int i = 0; i < op.Length; i++)
                        Advance();
                    return op;
                }
            }
            return null;
        }

        // Helpers

        private bool MatchAhead(string s)
        {
            if (_idx + s.Length > _src.Length) return false;
            for (int i = 0; i < s.Length; i++)
                if (_src[_idx + i] != s[i]) return false;
            return true;
        }

        private bool NextIsDigit()
        {
            return (_idx + 1 < _src.Length) && char.IsDigit(_src[_idx + 1]);
        }

        private void AddToken(TokenType type, string val)
        {
            _tokens.Add(new Token(type, val, _line, _col));
        }

        private bool IsAtEnd() => _idx >= _src.Length;
        private char Peek() => IsAtEnd() ? '\0' : _src[_idx];

        private void Advance()
        {
            if (IsAtEnd()) return;
            if (_src[_idx] == '\n')
            {
                _line++;
                _col = 1;
            }
            else
            {
                _col++;
            }
            _idx++;
        }
    }
}