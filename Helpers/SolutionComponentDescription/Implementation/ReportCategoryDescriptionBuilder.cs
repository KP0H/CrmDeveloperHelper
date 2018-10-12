using Microsoft.Xrm.Sdk.Query;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Helpers.SolutionComponentDescription.Implementation
{
    public class ReportCategoryDescriptionBuilder : DefaultSolutionComponentDescriptionBuilder
    {
        public ReportCategoryDescriptionBuilder(IOrganizationServiceExtented service)
            : base(service, (int)ComponentType.ReportCategory)
        {

        }

        public override ComponentType? ComponentTypeEnum => ComponentType.ReportCategory;

        public override int ComponentTypeValue => (int)ComponentType.ReportCategory;

        public override string EntityLogicalName => ReportCategory.EntityLogicalName;

        public override string EntityPrimaryIdAttribute => ReportCategory.Schema.EntityPrimaryIdAttribute;

        protected override ColumnSet GetColumnSet()
        {
            return new ColumnSet
                (
                    ReportCategory.Schema.Attributes.reportid
                    , ReportCategory.Schema.Attributes.categorycode
                    , ReportCategory.Schema.Attributes.ismanaged
                    , ReportCategory.Schema.Attributes.iscustomizable
                );
        }

        public override void GenerateDescription(StringBuilder builder, IEnumerable<SolutionComponent> components, bool withUrls)
        {
            var list = GetEntities<ReportCategory>(components.Select(c => c.ObjectId));

            {
                var hash = new HashSet<Guid>(list.Select(en => en.Id));
                var notFinded = components.Where(en => !hash.Contains(en.ObjectId.Value)).ToList();
                if (notFinded.Any())
                {
                    builder.AppendFormat(formatSpacer, unknowedMessage).AppendLine();
                    notFinded.ForEach(item => builder.AppendFormat(formatSpacer, item.ToString()).AppendLine());
                }
            }

            FormatTextTableHandler table = new FormatTextTableHandler();
            table.SetHeader("ReportName", "Category", "IsManaged", "SolutionName", "SolutionIsManaged", "SupportingName", "SupportinIsManaged");

            foreach (var entity in list)
            {
                var reportRef = entity.ReportId;
                string reportName = string.Empty;

                if (reportRef != null)
                {
                    reportName = reportRef.Name;
                }

                table.AddLine(reportName
                    , entity.FormattedValues[ReportCategory.Schema.Attributes.categorycode]
                    , entity.IsManaged.ToString()
                    , EntityDescriptionHandler.GetAttributeString(entity, "solution.uniquename")
                    , EntityDescriptionHandler.GetAttributeString(entity, "solution.ismanaged")
                    , EntityDescriptionHandler.GetAttributeString(entity, "suppsolution.uniquename")
                    , EntityDescriptionHandler.GetAttributeString(entity, "suppsolution.ismanaged")
                    );
            }

            List<string> lines = table.GetFormatedLines(true);

            lines.ForEach(item => builder.AppendFormat(formatSpacer, item).AppendLine());
        }

        public override string GenerateDescriptionSingle(SolutionComponent component, bool withUrls)
        {
            var reportCategory = GetEntity<ReportCategory>(component.ObjectId.Value);

            if (reportCategory != null)
            {
                var reportRef = reportCategory.ReportId;
                string reportName = string.Empty;

                if (reportRef != null)
                {
                    reportName = reportRef.Name;
                }

                return string.Format("Report {0}    Category {1}    IsManaged {2}    SolutionName {3}"
                    , reportName
                    , reportCategory.FormattedValues[ReportCategory.Schema.Attributes.categorycode]
                    , reportCategory.IsManaged.ToString()
                    , EntityDescriptionHandler.GetAttributeString(reportCategory, "solution.uniquename")
                    );
            }

            return component.ToString();
        }

        public override string GetName(SolutionComponent component)
        {
            var entity = GetEntity<ReportCategory>(component.ObjectId.Value);

            if (entity != null)
            {
                return string.Format("{0} - {1}", entity.ReportId?.Name, entity.FormattedValues.ContainsKey(ReportCategory.Schema.Attributes.categorycode) ? entity.FormattedValues[ReportCategory.Schema.Attributes.categorycode] : entity.CategoryCode?.Value.ToString());
            }

            return component.ObjectId.ToString();
        }
    }
}