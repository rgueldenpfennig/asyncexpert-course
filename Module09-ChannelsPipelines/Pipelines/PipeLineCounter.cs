using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pipelines
{
    public class PipeLineCounter
    {
        public async Task<int> CountLines(Uri uri)
        {
            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(uri);

            var countLines = 0;
            var pipeReder = PipeReader.Create(stream);

            ReadResult readResult;
            do
            {
                readResult = await pipeReder.ReadAsync();
                countLines += CountLines(readResult.Buffer, out long consumed);
                pipeReder.AdvanceTo(readResult.Buffer.GetPosition(consumed), readResult.Buffer.End);
            } while (!readResult.IsCompleted && !readResult.IsCanceled);

            //SequenceReader<>

            // Calculate how many lines (end of line characters `\n`) are in the network stream
            // To practice, use a pattern where you have the Pipe, Writer and Reader tasks
            // Read about SequenceReader<T>, https://docs.microsoft.com/en-us/dotnet/api/system.buffers.sequencereader-1?view=netcore-3.1
            // This struct h has a method that can be very useful for this scenario :)

            // Good luck and have fun with pipelines!

            return countLines;
        }

        private static int CountLines(in ReadOnlySequence<byte> sequence, out long consumed)
        {
            int countLines = 0;

            var seqReader = new SequenceReader<byte>(sequence);
            while (seqReader.TryAdvanceTo((byte)'\n'))
            {
                countLines++;
            }

            consumed = seqReader.Consumed;

            return countLines;
        }
    }
}
