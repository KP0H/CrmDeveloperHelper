﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces
{
    public interface ISolutionComponentDescriptionBuilder
    {
        int ComponentTypeValue { get; }

        ComponentType? ComponentTypeEnum { get; }

        string EntityLogicalName { get; }

        string EntityPrimaryIdAttribute { get; }

        List<T> GetEntities<T>(IEnumerable<Guid> components) where T : Entity;

        List<T> GetEntities<T>(IEnumerable<Guid?> components) where T : Entity;

        T GetEntity<T>(Guid idEntity) where T : Entity;

        void FillSolutionImageComponent(List<SolutionImageComponent> result, SolutionComponent solutionComponent);

        void GenerateDescription(StringBuilder builder, IEnumerable<SolutionComponent> components, bool withUrls);

        string GenerateDescriptionSingle(SolutionComponent solutionComponent, bool withUrls);

        string GetName(SolutionComponent solutionComponent);

        string GetDisplayName(SolutionComponent solutionComponent);

        string GetManagedName(SolutionComponent solutionComponent);

        string GetCustomizableName(SolutionComponent solutionComponent);

        string GetFileName(string connectionName, Guid objectId, string fieldTitle, string extension);

        TupleList<string, string> GetComponentColumns();
    }
}