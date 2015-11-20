using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests
{
    class Example
    {
        public async Task Test()
        {
            var asyncClient = new S3AsyncClient();
            var syncClient = new S3Client();

            // Instead of calling Invoke() / InvokeAsync() on the Command, callers use an
            // Invoker and pass the command to it. This does a few things for us:
            // - Injecting an ICommandInvoker for DI and unit testing is easy
            // - It helps differentiate between sync and async code paths with separate methods
            var invoker = new CommandInvoker();

            // Async and sync commands inherit from different Base classes.

            // Async example
            var fileExistsAsyncCommand = new S3FileExistsAsyncCommand(asyncClient, "static-content", "foo.txt");
            var result1 = await invoker.InvokeAsync(fileExistsAsyncCommand, OnFailure.Throw);
            var exists1 = result1.Value;


            // Sync example
            var fileExistsSyncCommand = new S3FileExistsCommand(syncClient, "static-content", "foo.txt");
            var result2 = invoker.Invoke(fileExistsSyncCommand, OnFailure.Throw, 1000);
            var exists2 = result2.Value;


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

        public S3FileExistsAsyncCommand(IS3AsyncClient client, string bucketName, string fileName)
            : base("s3", "s3-read", TimeSpan.FromSeconds(5))
        {
            if (client == null) throw new ArgumentNullException("client");
            // TODO other validation
            _bucketName = bucketName;
            _fileName = fileName;
        }

        protected internal override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
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

        protected internal override bool Execute(CancellationToken cancellationToken)
        {
            return _client.FileExists(_bucketName, _fileName);
        }
    }

    class S3UploadFileCommand : SyncCommand<VoidResult>
    {
        private readonly IS3Client _client;
        private readonly string _bucketName;
        private readonly string _localFile;
        private readonly string _key;
        private readonly string _contentType;
        private readonly string _statContentType;

        public S3UploadFileCommand(IS3Client client, string bucketName, string localFile, string key, string contentType, string statContentType)
            : base("s3", "s3-write", TimeSpan.FromSeconds(5))
        {
            _client = client;
            _bucketName = bucketName;
            _localFile = localFile;
            _key = key;
            _contentType = contentType;
            _statContentType = statContentType;
        }

        protected internal override VoidResult Execute(CancellationToken cancellationToken)
        {
            _client.UploadFile(_bucketName, _localFile, _key, _contentType, _statContentType);
            return new VoidResult();
        }
    }
}
