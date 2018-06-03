using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lidar_UI
{
    class JobRunner
    {
        const int workers = 2;

        public bool Cleanup = true;

        Repository repository;

        public ObservableCollection<Job> jobs;

        public JobRunner(Repository repository)
        {
            this.repository = repository;
        }

        public event EventHandler<Job> JobStarted;
        public event EventHandler<Job> JobFinished;

        public async Task RunArea(int x1, int y1, int x2, int y2)
        {
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
                        tiles[id] = repository.Tiles[id] = new Tile(id, repository.directory);
                    }
                    repository.UpdateTile(tiles[id]);
                }
            }

            BufferBlock<Tile> waitingQueue = new BufferBlock<Tile>(new DataflowBlockOptions() {
                EnsureOrdered = true
            });

            TransformBlock<Tile, Tile> workerBlock = new TransformBlock<Tile, Tile>(async t =>
            {
                var job = Job.NextJob(t);
                if (job == null) return t;
                JobStarted.Invoke(this, job);
                await job.Run(Cleanup);
                t.Rescan(repository.directory);
                JobFinished.Invoke(this, job);
                return t;
            }, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = workers,
            });

            ActionBlock<Tile> mapUpdater = new ActionBlock<Tile>(t => repository.UpdateTile(t));
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
            workerBlock.LinkTo(mapUpdater);

            foreach (var pair in tiles)
            {
                await waitingQueue.SendAsync(pair.Value);
            }

            await finishTask.Task;
        }
    }
}
