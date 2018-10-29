using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Helpers.SolutionComponentDescription.Implementation
{
    public class ChannelAccessProfileDescriptionBuilder : DefaultSolutionComponentDescriptionBuilder
    {
        public ChannelAccessProfileDescriptionBuilder(IOrganizationServiceExtented service)
            : base(service, (int)ComponentType.ChannelAccessProfile)
        {

        }

        public override ComponentType? ComponentTypeEnum => ComponentType.ChannelAccessProfile;

        public override int ComponentTypeValue => (int)ComponentType.ChannelAccessProfile;

        public override string EntityLogicalName => ChannelAccessProfile.EntityLogicalName;

        public override string EntityPrimaryIdAttribute => ChannelAccessProfile.Schema.EntityPrimaryIdAttribute;

        protected override ColumnSet GetColumnSet()
        {
            return new ColumnSet
                (
                    ChannelAccessProfile.Schema.Attributes.name
                    , ChannelAccessProfile.Schema.Attributes.statuscode
                    , ChannelAccessProfile.Schema.Attributes.ismanaged
                );
        }

        protected override FormatTextTableHandler GetDescriptionHeader(bool withUrls, bool withManaged, bool withSolutionInfo, Action<FormatTextTableHandler, bool, bool, bool> action)
        {
            FormatTextTableHandler handler = new FormatTextTableHandler();
            handler.SetHeader("Name", "StatusCode", "Id");

            action(handler, withUrls, withManaged, withSolutionInfo);

            return handler;
        }

        protected override List<string> GetDescriptionValues(Entity entityInput, bool withUrls, bool withManaged, bool withSolutionInfo, Action<List<string>, Entity, bool, bool, bool> action)
        {
            var entity = entityInput.ToEntity<ChannelAccessProfile>();

            List<string> values = new List<string>();

            values.AddRange(new[]
            {
                entity.Name
                , entity.FormattedValues[ChannelAccessProfile.Schema.Attributes.statuscode]
                , entity.Id.ToString()
            });

            action(values, entity, withUrls, withManaged, withSolutionInfo);

            return values;
        }

        public override TupleList<string, string> GetComponentColumns()
        {
            return new TupleList<string, string>
                {
                    { ChannelAccessProfile.Schema.Attributes.name, "Name" }
                    , { ChannelAccessProfile.Schema.Attributes.statuscode, "StatusCode" }
                    , { ChannelAccessProfile.Schema.Attributes.ismanaged, "IsManaged" }
                    , { "solution.uniquename", "SolutionName" }
                    , { "solution.ismanaged", "SolutionIsManaged" }
                    , { "suppsolution.uniquename", "SupportingName" }
                    , { "suppsolution.ismanaged", "SupportingIsManaged" }
                };
        }
    }
}