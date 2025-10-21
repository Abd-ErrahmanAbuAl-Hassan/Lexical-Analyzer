using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scanner
{
    public static class CDefinitions
    {
        public static readonly HashSet<string> Keywords = new()
    {
         "auto","break","case","char","const","continue","default","do","double",
        "else","enum","extern","float","for","goto","if","inline","int","long",
        "register","restrict","return","short","signed","sizeof","static","struct",
        "switch","typedef","union","unsigned","void","volatile","while",
        "_Bool","_Complex","_Imaginary","main"
    };

        public static readonly HashSet<string> Operators = new()
    {
       // Assignment compounds (longer first)
        "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",
        // Shift
        "<<", ">>",
        // Increment/decrement
        "++", "--",
        // Relational - multi char
        "==", "!=", "<=", ">=",
        // Logical
        "&&", "||",
        // Single-char operators and other bitwise
        "+", "-", "*", "/", "%", "=", "!", "<", ">", "&", "|", "^", "~"
    };

        public static readonly HashSet<char> Delimiters = new()
    {
        ';', ',', '(', ')', '{', '}', '[', ']'
    };
    }

}
