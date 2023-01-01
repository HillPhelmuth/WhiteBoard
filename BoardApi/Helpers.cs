using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Board.Api
{
    public static class Helpers
    {
        public static async Task<byte[]> ReadFully(this Stream input)
        {
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            return ms.ToArray();
        }
        public static string NoFileExt(this string file)
        {
            return file.Substring(0, file.LastIndexOf('.'));
        }
        public static string ToValidContainerName(this string str)
        {
            var sb = new StringBuilder();
            foreach (char c in str.Where(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '-'))
            {
                sb.Append(c);
            }
            return sb.ToString().ToLower();
        }
    }
}