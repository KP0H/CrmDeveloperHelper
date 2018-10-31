using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using Nav.Common.VSPackages.CrmDeveloperHelper.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Helpers.SolutionComponentDescription.Implementation
{
    public class RoleDescriptionBuilder : DefaultSolutionComponentDescriptionBuilder
    {
        public RoleDescriptionBuilder(IOrganizationServiceExtented service)
            : base(service, (int)ComponentType.Role)
        {

        }

        public override ComponentType? ComponentTypeEnum => ComponentType.Role;

        public override int ComponentTypeValue => (int)ComponentType.Role;

        public override string EntityLogicalName => Role.EntityLogicalName;

        public override string EntityPrimaryIdAttribute => Role.Schema.EntityPrimaryIdAttribute;

        protected override ColumnSet GetColumnSet()
        {
            return new ColumnSet(Role.Schema.Attributes.name, Role.Schema.Attributes.businessunitid, Role.Schema.Attributes.ismanaged, Role.Schema.Attributes.iscustomizable);
        }

        protected override QueryExpression GetQuery(List<Guid> idsNotCached)
        {
            var query = new QueryExpression()
            {
                NoLock = true,

                EntityName = Role.EntityLogicalName,

                ColumnSet = GetColumnSet(),

                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(this.EntityPrimaryIdAttribute, ConditionOperator.In, idsNotCached.ToArray()),
                    },
                },

                LinkEntities =
                {
                    new LinkEntity()
                    {
                        JoinOperator = JoinOperator.LeftOuter,

                        LinkFromEntityName = Role.EntityLogicalName,
                        LinkFromAttributeName = Role.Schema.Attributes.businessunitid,

                        LinkToEntityName = BusinessUnit.EntityLogicalName,
                        LinkToAttributeName = BusinessUnit.PrimaryIdAttribute,

                        EntityAlias = BusinessUnit.EntityLogicalName,

                        Columns = new ColumnSet(BusinessUnit.Schema.Attributes.parentbusinessunitid, BusinessUnit.Schema.Attributes.name),
                    },

                    new LinkEntity()
                    {
                        JoinOperator = JoinOperator.LeftOuter,

                        LinkFromEntityName = Role.EntityLogicalName,
                        LinkFromAttributeName = Role.Schema.Attributes.roletemplateid,

                        LinkToEntityName = RoleTemplate.Schema.EntityLogicalName,
                        LinkToAttributeName = RoleTemplate.Schema.EntityPrimaryIdAttribute,

                        EntityAlias = RoleTemplate.Schema.EntityLogicalName,

                        Columns = new ColumnSet(RoleTemplate.Schema.Attributes.name),
                    },

                    new LinkEntity()
                    {
                        JoinOperator = JoinOperator.LeftOuter,

                        LinkFromEntityName = Role.EntityLogicalName,
                        LinkFromAttributeName = Role.Schema.Attributes.solutionid,

                        LinkToEntityName = Solution.EntityLogicalName,
                        LinkToAttributeName = Solution.PrimaryIdAttribute,

                        EntityAlias = Solution.EntityLogicalName,

                        Columns = new ColumnSet(Solution.Schema.Attributes.uniquename, Solution.Schema.Attributes.ismanaged),
                    },

                    new LinkEntity()
                    {
                        JoinOperator = JoinOperator.LeftOuter,

                        LinkFromEntityName = Role.EntityLogicalName,
                        LinkFromAttributeName = Role.Schema.Attributes.supportingsolutionid,

                        LinkToEntityName = Solution.EntityLogicalName,
                        LinkToAttributeName = Solution.PrimaryIdAttribute,

                        EntityAlias = SupportingSolutionAlias,

                        Columns = new ColumnSet(Solution.Schema.Attributes.uniquename, Solution.Schema.Attributes.ismanaged),
                    },
                },

                Orders =
                {
                    new OrderExpression(Role.Schema.Attributes.name, OrderType.Ascending),
                },

                PageInfo = new PagingInfo()
                {
                    PageNumber = 1,
                    Count = 5000,
                }
            };

            return query;
        }

        protected override FormatTextTableHandler GetDescriptionHeader(bool withUrls, bool withManaged, bool withSolutionInfo, Action<FormatTextTableHandler, bool, bool, bool> action)
        {
            FormatTextTableHandler handler = new FormatTextTableHandler();
            handler.SetHeader("Name", "RoleTemplate", "BusinessUnit", "IsCustomizable");

            action(handler, withUrls, withManaged, withSolutionInfo);

            return handler;
        }

        protected override List<string> GetDescriptionValues(Entity entityInput, bool withUrls, bool withManaged, bool withSolutionInfo, Action<List<string>, Entity, bool, bool, bool> action)
        {
            var entity = entityInput.ToEntity<Role>();

            List<string> values = new List<string>();

            var businessUnit = entity.BusinessUnitId.Name;

            if (entity.BusinessUnitParentBusinessUnit == null)
            {
                businessUnit = "Root Organization";
            }

            values.AddRange(new[]
            {
                entity.Name
                , EntityDescriptionHandler.GetAttributeString(entity, RoleTemplate.Schema.EntityLogicalName + "." + RoleTemplate.Schema.Attributes.name)
                , businessUnit
                , entity.IsCustomizable?.Value.ToString()
            });

            action(values, entity, withUrls, withManaged, withSolutionInfo);

            return values;
        }

        public override void FillSolutionImageComponent(ICollection<SolutionImageComponent> result, SolutionComponent solutionComponent)
        {
            if (solutionComponent == null
                || !solutionComponent.ObjectId.HasValue
                )
            {
                return;
            }

            var entity = GetEntity<Role>(solutionComponent.ObjectId.Value);

            if (entity != null)
            {
                var imageComponent = new SolutionImageComponent()
                {
                    ComponentType = this.ComponentTypeValue,
                    RootComponentBehavior = (solutionComponent.RootComponentBehavior?.Value).GetValueOrDefault((int)RootComponentBehavior.IncludeSubcomponents),

                    Description = GenerateDescriptionSingle(solutionComponent, false, true, true),
                };

                if (entity.RoleTemplateId != null)
                {
                    imageComponent.SchemaName = entity.RoleTemplateId.Id.ToString();
                }
                else
                {
                    imageComponent.ObjectId = entity.Id;
                }

                result.Add(imageComponent);
            }
        }

        public override void FillSolutionComponent(ICollection<SolutionComponent> result, SolutionImageComponent solutionImageComponent)
        {
            if (solutionImageComponent == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(solutionImageComponent.SchemaName)
                && Guid.TryParse(solutionImageComponent.SchemaName, out var roleTemplateId))
            {
                var repository = new RoleRepository(_service);

                var entity = repository.FindRoleByTemplate(roleTemplateId, new ColumnSet(false));

                if (entity != null)
                {
                    var component = new SolutionComponent()
                    {
                        ComponentType = new OptionSetValue(this.ComponentTypeValue),
                        ObjectId = entity.Id,
                        RootComponentBehavior = new OptionSetValue((int)RootComponentBehavior.IncludeSubcomponents),
                    };

                    if (solutionImageComponent.RootComponentBehavior.HasValue)
                    {
                        component.RootComponentBehavior = new OptionSetValue(solutionImageComponent.RootComponentBehavior.Value);
                    }

                    result.Add(component);

                    return;
                }
            }

            base.FillSolutionComponent(result, solutionImageComponent);
        }

        public override TupleList<string, string> GetComponentColumns()
        {
            return new TupleList<string, string>
                {
                    {  Role.Schema.Attributes.name, "Name" }
                    , { RoleTemplate.Schema.EntityLogicalName + "." + RoleTemplate.Schema.Attributes.name, "RoleTemplate" }
                    , { Role.Schema.Attributes.businessunitid, "BusinessUnit" }
                    , { Role.Schema.Attributes.iscustomizable, "IsCustomizable" }
                    , { Role.Schema.Attributes.ismanaged, "IsManaged" }
                    , { "solution.uniquename", "SolutionName" }
                    , { "solution.ismanaged", "SolutionIsManaged" }
                    , { "suppsolution.uniquename", "SupportingName" }
                    , { "suppsolution.ismanaged", "SupportingIsManaged" }
                };
        }
    }
}