using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lidar_UI
{
    class JobRunner
    {
        const int workers = 6;

        public static bool Cleanup = false;

        Repository repository;

        public ObservableCollection<Job> jobs;

        public JobRunner(Repository repository)
        {
            this.repository = repository;
        }

        public event EventHandler<Job> JobStarted;
        public event EventHandler<Job> JobFinished;
        CancellationTokenSource cancellationSource;

        public static bool download, color, normals, water;

        public async Task RunArea(int x1, int y1, int x2, int y2)
        {
            cancellationSource = new CancellationTokenSource();
            jobs = new ObservableCollection<Job>();
            Dictionary<TileId, Tile> tiles = new Dictionary<TileId, Tile>();
            for(int x = x1; x <= x2; x++)
            {
                for(int y = y1; y <= y2; y++)
                {
                    TileId id = new TileId(x, y);
                    if (repository.Tiles.ContainsKey(id))
                    {
                        tiles[id] = repository.Tiles[id];
                    }
                    else
                    {
                        var tile = repository.GenerateTile(id);
                        tiles[id] = repository.Tiles[id] = tile;
                    }
                    repository.UpdateTile(tiles[id]);
                }
            }

            BufferBlock<Tile> waitingQueue = new BufferBlock<Tile>(new DataflowBlockOptions() {
                EnsureOrdered = true
            });
            
            TransformBlock<Tile, Tile> workerBlock = new TransformBlock<Tile, Tile>(async t =>
            {
                t.Rescan(repository.DirectoryForTile(t.id));
                var job = Job.NextJob(t);
                if (job == null) return t;
                JobStarted.Invoke(this, job);
                await job.Run(cancellationSource.Token, Cleanup);
                t.Rescan(repository.DirectoryForTile(t.id));
                JobFinished.Invoke(this, job);
                return t;
            }, new ExecutionDataflowBlockOptions()
            {
                CancellationToken = cancellationSource.Token,
                MaxDegreeOfParallelism = workers,
            });

            //ActionBlock<Tile> mapUpdater = new ActionBlock<Tile>(t => repository.UpdateTile(t));
            TaskCompletionSource<int> finishTask = new TaskCompletionSource<int>();
            int finishedCounter = tiles.Count;
            ActionBlock<Tile> tileFinished = new ActionBlock<Tile>(t => {
                finishedCounter--;
                if(finishedCounter == 0)
                {
                    finishTask.SetResult(finishedCounter);
                }
            });

            waitingQueue.LinkTo(workerBlock);
            workerBlock.LinkTo(waitingQueue, t => Job.NextJob(t) != null);
            workerBlock.LinkTo(tileFinished, t => Job.NextJob(t) == null);
            //workerBlock.LinkTo(mapUpdater);

            foreach (var pair in tiles)
            {
                await waitingQueue.SendAsync(pair.Value);
            }

            await finishTask.Task;
        }

        internal void Close()
        {
            cancellationSource?.Cancel();
            Thread.Sleep(100);
        }
    }
}
