namespace CardStorageRestAPI
{
    public class AsciiIdentifier
    {
        private readonly string verifiedString;
        public AsciiIdentifier(string str) {
            // checking the input for permitted values

            var trimmed = str.Trim();
            // only A-Za-z0-9 and '_' and '-' are permitted
            foreach (char c in trimmed) {
                if (
                    (c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    (c == '-') || (c == '_')
                    )
                    continue;
                throw new ArgumentException($"identifier contains characters that are not permitted. Only A-Za-z0-9 and '_' and '-' are permitted");
            }
            verifiedString = trimmed;
        }

        public override string ToString()
        {
            return this.verifiedString;
        }


    }
}
