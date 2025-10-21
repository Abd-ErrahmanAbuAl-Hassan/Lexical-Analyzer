namespace Scanner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string code = @"
int main() {
    int z = 5;
    float x = 3.14;
    float y = .5e-1;
    // single line comment
    /* multi
       line comment */
    x += y * 2;
    if (x >= 1.0 && x != 0) x++; //another comment
    return 0;
}
";

            var scanner = new Scanner(code, keepComments: false);
            var tokens = scanner.Scan();

            foreach (var token in tokens)
                Console.WriteLine(token);

        }
    }
}
