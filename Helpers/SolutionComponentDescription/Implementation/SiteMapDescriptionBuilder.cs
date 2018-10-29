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
    public class SiteMapDescriptionBuilder : DefaultSolutionComponentDescriptionBuilder
    {
        public SiteMapDescriptionBuilder(IOrganizationServiceExtented service)
            : base(service, (int)ComponentType.SiteMap)
        {

        }

        public override ComponentType? ComponentTypeEnum => ComponentType.SiteMap;

        public override int ComponentTypeValue => (int)ComponentType.SiteMap;

        public override string EntityLogicalName => SiteMap.EntityLogicalName;

        public override string EntityPrimaryIdAttribute => SiteMap.Schema.EntityPrimaryIdAttribute;

        protected override ColumnSet GetColumnSet()
        {
            return new ColumnSet
            (
                SiteMap.Schema.Attributes.sitemapname
                , SiteMap.Schema.Attributes.sitemapnameunique
                , SiteMap.Schema.Attributes.sitemapid
                , SiteMap.Schema.Attributes.isappaware
                , SiteMap.Schema.Attributes.ismanaged
            );
        }

        protected override FormatTextTableHandler GetDescriptionHeader(bool withUrls, bool withManaged, bool withSolutionInfo, Action<FormatTextTableHandler, bool, bool, bool> action)
        {
            FormatTextTableHandler handler = new FormatTextTableHandler();
            handler.SetHeader("SiteMapName", "SiteMapNameUnique", "Id", "IsAppAware");

            action(handler, withUrls, withManaged, withSolutionInfo);

            return handler;
        }

        protected override List<string> GetDescriptionValues(Entity entityInput, bool withUrls, bool withManaged, bool withSolutionInfo, Action<List<string>, Entity, bool, bool, bool> action)
        {
            var entity = entityInput.ToEntity<SiteMap>();

            List<string> values = new List<string>();

            values.AddRange(new[]
            {
                entity.SiteMapName
                , entity.SiteMapNameUnique
                , entity.Id.ToString()
                , entity.IsAppAware.ToString()
            });

            action(values, entity, withUrls, withManaged, withSolutionInfo);

            return values;
        }

        public override string GetName(SolutionComponent component)
        {
            var entity = GetEntity<SiteMap>(component.ObjectId.Value);

            if (entity != null)
            {
                return entity.SiteMapNameUnique ?? entity.SiteMapName ?? entity.Id.ToString();
            }

            return base.GetName(component);
        }

        public override void FillSolutionImageComponent(ICollection<SolutionImageComponent> result, SolutionComponent solutionComponent)
        {
            if (solutionComponent == null
                || !solutionComponent.ObjectId.HasValue
                )
            {
                return;
            }

            var entity = GetEntity<SiteMap>(solutionComponent.ObjectId.Value);

            if (entity != null)
            {
                var imageComponent = new SolutionImageComponent()
                {
                    ComponentType = this.ComponentTypeValue,

                    RootComponentBehavior = (solutionComponent.RootComponentBehavior?.Value).GetValueOrDefault((int)RootComponentBehavior.IncludeSubcomponents),

                    Description = GenerateDescriptionSingle(solutionComponent, false, true, true),
                };

                if (!string.IsNullOrEmpty(entity.SiteMapNameUnique))
                {
                    imageComponent.SchemaName = entity.SiteMapNameUnique;
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

            string sitemapName = solutionImageComponent.SchemaName;

            var repository = new SitemapRepository(_service);

            var entity = repository.FindByExactName(sitemapName, new ColumnSet(false));

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

        public override TupleList<string, string> GetComponentColumns()
        {
            return new TupleList<string, string>
                {
                    { SiteMap.Schema.EntityPrimaryIdAttribute, "Id" }
                    , { SiteMap.Schema.Attributes.sitemapname, "Name" }
                    , { SiteMap.Schema.Attributes.sitemapnameunique, "NameUnique" }
                    , { SiteMap.Schema.Attributes.isappaware, "IsAppware" }
                    , { SiteMap.Schema.Attributes.ismanaged, "IsManaged" }
                    , { "solution.uniquename", "SolutionName" }
                    , { "solution.ismanaged", "SolutionIsManaged" }
                    , { "suppsolution.uniquename", "SupportingName" }
                    , { "suppsolution.ismanaged", "SupportingIsManaged" }
                };
        }
    }
}