using Database.Models;
using DataModels.Interfaces;
using DataModels.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Implements
{
    public class ReadStorage: IReadStorage
    {
        private readonly ILogger<ReadStorage> _logger;

        public ReadStorage(ILogger<ReadStorage> logger)
        {
            _logger = logger;
        }

        public async Task<List<ReadModel>> GetFullList()
        {
            _logger.LogInformation($"Get full read list request received");

            try
            {
                using var context = new Database();
                var result = await context.Reads.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get full read list success");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get full read list failed, unexpected error");
                throw;
            }
        }

        public async Task<List<ReadModel>> GetFilteredList(ReadSearchModel model)
        {
            _logger.LogInformation($"Get filtered read list request received, readID={model?.Id}, projectID={model?.ProjectId}, sampleName={model?.SampleName}, instrumentModel={model?.InstrumentModel}");

            try
            {
                using var context = new Database();

                var query = context.Reads.AsQueryable();

                if (model.Id.HasValue)
                {
                    query = query.Where(x => x.Id == model.Id.Value);
                }
                if (model.ProjectId.HasValue)
                {
                    query = query.Where(x => x.ProjectId == model.ProjectId.Value);
                }
                if (!string.IsNullOrEmpty(model.SampleName))
                {
                    query = query.Where(x => x.SampleName.Equals(model.SampleName));
                }
                if (!string.IsNullOrEmpty(model.InstrumentModel))
                {
                    query = query.Where(x => x.InstrumentModel.Equals(model.InstrumentModel));
                }
                //TODO: make filter by created date and notes (maybe)

                var result = await query.Select(x => x.GetViewModel).ToListAsync();

                _logger.LogInformation($"Get filtered read list success, readID={model.Id}, projectID={model.ProjectId}, sampleName={model.SampleName}, instrumentModel={model.InstrumentModel}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get filtered read list failed, unexpected error, readID={model?.Id}, projectID={model?.ProjectId}, sampleName={model?.SampleName}, instrumentModel={model?.InstrumentModel}");
                throw;
            }
        }

        public async Task<ReadModel?> GetElement(ReadSearchModel model)
        {
            _logger.LogInformation($"Get read element request received, readID={model?.Id}, projectID={model?.ProjectId}, sampleName={model?.SampleName}, instrumentModel={model?.InstrumentModel}");

            try
            {
                using var context = new Database();

                if (model.Id.HasValue)
                {
                    var read = await context.Reads.FirstOrDefaultAsync(x => x.Id == model.Id.Value);

                    if (read == null)
                    {
                        _logger.LogWarning($"Get read element failed, read not found, readID={model.Id}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get read element success, readID={model.Id}");
                    }

                    return read?.GetViewModel;
                }

                if (model.ProjectId.HasValue)
                {
                    var read = await context.Reads.FirstOrDefaultAsync(x => x.ProjectId == model.ProjectId.Value);

                    if (read == null)
                    {
                        _logger.LogWarning($"Get read element failed, read not found, projectID={model.ProjectId}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get read element success, projectID={model.ProjectId}");
                    }

                    return read?.GetViewModel;
                }


                if (!string.IsNullOrEmpty(model.SampleName))
                {
                    var read = await context.Reads.FirstOrDefaultAsync(x => x.SampleName.Equals(model.SampleName));

                    if (read == null)
                    {
                        _logger.LogWarning($"Get read element failed, read not found, sampleName={model.SampleName}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get read element success, sampleName={model.SampleName}");
                    }

                    return read?.GetViewModel;
                }

                if (!string.IsNullOrEmpty(model.InstrumentModel))
                {
                    var read = await context.Reads.FirstOrDefaultAsync(x => x.InstrumentModel.Equals(model.InstrumentModel));

                    if (read == null)
                    {
                        _logger.LogWarning($"Get read element failed, read not found, instrumentModel={model.InstrumentModel}");
                    }
                    else
                    {
                        _logger.LogInformation($"Get read element success, instrumentModel={model.InstrumentModel}");
                    }

                    return read?.GetViewModel;
                }
                //TODO: make filter by created date and notes (maybe again)

                _logger.LogWarning($"Get read element failed, search parameters are empty");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get read element failed, unexpected error, readID={model?.Id}, projectID={model?.ProjectId}, sampleName={model?.SampleName}, instrumentModel={model?.InstrumentModel}");
                throw;
            }
        }

        public async Task<ReadModel?> Insert(ReadModel model)
        {
            _logger.LogInformation($"Insert read request received, projectID={model?.ProjectId}, sampleName={model?.SampleName}, fileName={model?.FileName}");

            try
            {
                var read = Read.Create(model);
                if (read == null)
                {
                    _logger.LogWarning($"Insert read failed, read create returned null, projectID={model?.ProjectId}, sampleName={model?.SampleName}, fileName={model?.FileName}");
                    return null;
                }
                using var context = new Database();
                await context.Reads.AddAsync(read);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Insert read success, readID={read.Id}, projectID={model.ProjectId}, sampleName={model.SampleName}, fileName={model.FileName}");
                return read.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Insert read failed, unexpected error, projectID={model?.ProjectId}, sampleName={model?.SampleName}, fileName={model?.FileName}");
                throw;
            }
        }

        public async Task<ReadModel?> Update(ReadModel model)
        {
            _logger.LogInformation($"Update read request received, readID={model?.Id}, projectID={model?.ProjectId}, sampleName={model?.SampleName}");

            try
            {
                using var context = new Database();
                var read = await context.Reads.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (read == null)
                {
                    _logger.LogWarning($"Update read failed, read not found, readID={model.Id}");
                    return null;
                }
                read.Update(model);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Update read success, readID={model.Id}");
                return read.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update read failed, unexpected error, readID={model?.Id}, projectID={model?.ProjectId}, sampleName={model?.SampleName}");
                throw;
            }
        }

        public async Task<ReadModel?> Delete(int id)
        {
            _logger.LogInformation($"Delete read request received, readID={id}");

            try
            {
                using var context = new Database();
                var read = await context.Reads.FirstOrDefaultAsync(x => x.Id == id);
                if (read == null)
                {
                    _logger.LogWarning($"Delete read failed, read not found, readID={id}");
                    return null;
                }
                context.Reads.Remove(read);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Delete read success, readID={id}");
                return read.GetViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete read failed, unexpected error, readID={id}");
                throw;
            }
        }
    }
}