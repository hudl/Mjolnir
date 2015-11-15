using Hudl.Mjolnir.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExampleConsole
{
    // converting async to sync is challenging
    //  - the question: do we make this easy, or put the decision on the caller every time?
    //  - even if we try and get all top-level support for async, we still have things like ActionFilters
    //  - are those enough of a one-off case to have the caller handle the conversion?
    // converting sync to async is easy (Task.FromResult())

    // Goals / Changes:
    // - Everything uses semaphores by default (both sync and async limits)
    // - Commands invocation is more easily mocked for unit and behavioral testing
    //   - Testing failure cases is also straightforward and not hidden or hard to do
    // - Support for non-generic Tasks and void methods

    // Secondary goals:
    // - Easier visibility into state; can we fire metrics off somewhere? A built-in page?

    // Implementation changes:
    // - Commands won't get queued anymore. Queueing does help absorb spikes, but has some
    //   downsides. It makes the execution state of a command harder to reason about; is my
    //   the command executing or stuck in the queue? If executing items are hanging, will
    //   the command ever dequeue? It also adds a significant bit of complexity into the
    //   code, and makes semaphore isolation difficult.
    //
    //   The drawback to removing the queue is that, to achieve the same behavior (roughly),
    //   existing concurrency limits for bulkheads have to be increased.

    // Feedback/Discussion/Review:
    // - In the S3 example here, is injecting the S3 client (via constructor) okay? Or is
    //   that cumbersome? It makes testing a lot easier, but if the client establishes a
    //   persistent connection, should that be something the command protects against? It's
    //   probably situation depending on what exactly the client does when it's created.
    //   An alternative would be to have a separate command for creating the client.
    // - Are there any terribly inefficient places where we're constructing a lot of objects
    //   on the main, higher-volume code paths here?
    

    class Example
    {
        public async Task Test()
        {
            var asyncClient = new S3AsyncClient();
            var syncClient = new S3Client();

            var executor = new CommandInvoker();

            var fileExistsAsyncCommand = new S3FileExistsAsyncCommand(asyncClient, "static-content", "foo.txt");
            var r1 = await executor.InvokeAsync(fileExistsAsyncCommand);
            //var r1 = await fileExistsAsyncCommand.InvokeAsync();

            var fileExistsSyncCommand = new S3FileExistsCommand(syncClient, "static-content", "foo.txt");
            var r2 = executor.Invoke(fileExistsSyncCommand);
            //var r2 = fileExistsSyncCommand.Invoke();

            // TODO get some other (non-S3) real-world examples of commands in here
        }
    }

    interface IS3Client // TODO not really a client, more of a wrapper
    {
        bool FileExists(string bucketName, string fileName);
        bool FolderExists(string bucketName, string folderName);
        Uri DownloadFile(string bucketName, string fileName, Uri localFile = null);
        void UploadFile(string bucketName, string localFile, string key, string contentType, string statContentType);
    }

    class S3Client : IS3Client
    {
        public Uri DownloadFile(string bucketName, string fileName, Uri localFile = null)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string bucketName, string fileName)
        {
            throw new NotImplementedException();
        }

        public bool FolderExists(string bucketName, string folderName)
        {
            throw new NotImplementedException();
        }

        public void UploadFile(string bucketName, string localFile, string key, string contentType, string statContentType)
        {
            throw new NotImplementedException();
        }
    }

    interface IS3AsyncClient
    {
        Task<bool> FileExistsAsync(string bucketName, string fileName);
    }

    class S3AsyncClient : IS3AsyncClient
    {
        public Task<bool> FileExistsAsync(string bucketName, string fileName)
        {
            throw new NotImplementedException();
        }
    }

    class S3FileExistsAsyncCommand : AsyncCommand<bool>
    {
        private readonly IS3AsyncClient _client;
        private readonly string _bucketName;
        private readonly string _fileName;

        public S3FileExistsAsyncCommand(IS3AsyncClient client, string bucketName, string fileName) : base("s3", "s3-read", TimeSpan.FromSeconds(5))
        {
            if (client == null) throw new ArgumentNullException("client");
            // TODO other validation
            _bucketName = bucketName;
            _fileName = fileName;
        }

        protected override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            return _client.FileExistsAsync(_bucketName, _fileName);
        }
    }

    class S3FileExistsCommand : SyncCommand<bool>
    {
        private readonly IS3Client _client;
        private readonly string _bucketName;
        private readonly string _fileName;

        public S3FileExistsCommand(IS3Client client, string bucketName, string fileName)
            : base("s3", "s3-read", TimeSpan.FromSeconds(5))
        {
            _client = client;
            _bucketName = bucketName;
            _fileName = fileName;
        }

        protected override bool Execute(CancellationToken cancellationToken)
        {
            return _client.FileExists(_bucketName, _fileName);
        }
    }

    class S3UploadFileCommand : SyncCommand
    {
        private readonly IS3Client _client;
        private readonly string _bucketName;
        private readonly string _localFile;
        private readonly string _key;
        private readonly string _contentType;
        private readonly string _statContentType;

        public S3UploadFileCommand(IS3Client client, string bucketName, string localFile, string key, string contentType, string statContentType)
            : base ("s3", "s3-write", TimeSpan.FromSeconds(5))
        {
            _client = client;
            _bucketName = bucketName;
            _localFile = localFile;
            _key = key;
            _contentType = contentType;
            _statContentType = statContentType;
        }

        protected override void Execute(CancellationToken cancellationToken)
        {
            _client.UploadFile(_bucketName, _localFile, _key, _contentType, _statContentType);
        }
    }
}
