﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Repository
{
    public class EntityMapRepository
    {
        /// <summary>
        /// Сервис CRM
        /// </summary>
        private IOrganizationServiceExtented _service;

        /// <summary>
        /// Конструктор репозитория
        /// </summary>
        /// <param name="service"></param>
        public EntityMapRepository(IOrganizationServiceExtented service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task<List<EntityMap>> GetListAsync()
        {
            return Task.Run(() => GetList());
        }

        private List<EntityMap> GetList()
        {
            QueryExpression query = new QueryExpression()
            {
                EntityName = EntityMap.EntityLogicalName,

                NoLock = true,

                ColumnSet = new ColumnSet(true),

                Orders =
                {
                    new OrderExpression(EntityMap.Schema.Attributes.sourceentityname, OrderType.Ascending),
                    new OrderExpression(EntityMap.Schema.Attributes.targetentityname, OrderType.Ascending),
                },

                PageInfo = new PagingInfo()
                {
                    PageNumber = 1,
                    Count = 5000,
                }
            };

            var result = new List<EntityMap>();

            try
            {
                while (true)
                {
                    var coll = _service.RetrieveMultiple(query);

                    result.AddRange(coll.Entities.Select(e => e.ToEntity<EntityMap>()));

                    if (!coll.MoreRecords)
                    {
                        break;
                    }

                    query.PageInfo.PagingCookie = coll.PagingCookie;
                    query.PageInfo.PageNumber++;
                }
            }
            catch (Exception ex)
            {
                Nav.Common.VSPackages.CrmDeveloperHelper.Helpers.DTEHelper.WriteExceptionToOutput(ex);
            }

            return result;
        }
    }
}
