using CSharpFunctionalExtensions;
using DirectoryService.Core.Locations;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DirectoryService.Core.Statistics
{
    internal class StatisticsService : IStatisticsService
    {
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly ILogger<StatisticsService> _logger;
        public StatisticsService(
            IStatisticsRepository statisticsRepository,
            ILogger<StatisticsService> logger)
        {
            _statisticsRepository = statisticsRepository;
            _logger = logger;
        }

        /// <summary>
        /// создать запись статистики
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="objectTypeName"></param>
        /// <param name="level"></param>
        /// <param name="action"></param>
        /// <param name="description"></param>
        /// <param name="parentId"></param>
        /// <param name="parentTypeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<Guid, Error>> CreateAsync(
            Guid objectId,
            string objectTypeName,
            Statistica.Level level,
            Statistica.Action action,
            string description,
            Guid? parentId,
            string? parentTypeName,
            CancellationToken cancellationToken)
        {
            Statistica newObj = Statistica.Create(
                objectId,
                objectTypeName,
                level,
                action,
                description,
                parentId,
                parentTypeName);

            var result = await _statisticsRepository.AddAsync(newObj, cancellationToken).ConfigureAwait(false);
            if(result.IsFailure)
            {
                _logger.LogError("Error creating record of Statictica");
                return GeneralErrors.Failure("ошибка запроса на создание");
            }
            return result.Value;
        }
        public Task<Result<Guid, Error>> CreateAsync(
            Guid objectId,
            string objectTypeName,
            Statistica.Level level,
            Statistica.Action action,
            string description,
            CancellationToken cancellationToken)
        {
            return CreateAsync(
            objectId,
            objectTypeName,
            level,
            action,
            description,
            null,
            null,
            cancellationToken);
        }
        /// <summary>
        /// получить записи статистики по Id объекта
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<List<Statistica>, Error>> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken)
        {
            var result = await _statisticsRepository.GetByObjectIdAsync(objectId, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                _logger.LogError("Request error");
                return GeneralErrors.Failure("ошибка запроса");
            }
            return result.Value;
        }
        /// <summary>
        /// получить записи статистики по Id родительского объекта
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<List<Statistica>, Error>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken)
        {
            var result = await _statisticsRepository.GetByParentIdAsync(parentId, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                _logger.LogError("Request error");
                return GeneralErrors.Failure("ошибка запроса");
            }
            return result.Value;
        }
    }
}
