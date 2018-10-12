﻿namespace Nav.Common.VSPackages.CrmDeveloperHelper.Entities
{
    public enum ComponentType
    {
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Entity = 1,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Attribute = 2,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Relationship = 3,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        AttributePicklistValue = 4,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        AttributeLookupValue = 5,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ViewAttribute = 6,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        LocalizedLabel = 7,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RelationshipExtraCondition = 8,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        OptionSet = 9,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        EntityRelationship = 10,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        EntityRelationshipRole = 11,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        EntityRelationshipRelationships = 12,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ManagedProperty = 13,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        EntityKey = 14,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        EntityKeyAttribute = 15,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Privilege = 16,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        PrivilegeObjectTypeCode = 17,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Index = 18,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Role = 20,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RolePrivileges = 21,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        DisplayString = 22,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        DisplayStringMap = 23,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Form = 24,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Organization = 25,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SavedQuery = 26,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Workflow = 29,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ProcessTrigger = 30,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Report = 31,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ReportEntity = 32,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ReportCategory = 33,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ReportVisibility = 34,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Attachment = 35,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        EmailTemplate = 36,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ContractTemplate = 37,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        KbArticleTemplate = 38,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        MailMergeTemplate = 39,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        DuplicateRule = 44,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        DuplicateRuleCondition = 45,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        EntityMap = 46,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        AttributeMap = 47,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RibbonCommand = 48,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RibbonContextGroup = 49,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RibbonCustomization = 50,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RibbonRule = 52,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RibbonTabToCommandMap = 53,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RibbonDiff = 55,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SavedQueryVisualization = 59,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SystemForm = 60,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        WebResource = 61,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SiteMap = 62,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ConnectionRole = 63,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ComplexControl = 64,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        FieldSecurityProfile = 70,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        FieldPermission = 71,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        AppModule = 80,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        AppModuleRoles = 88,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        PluginType = 90,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        PluginAssembly = 91,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SdkMessageProcessingStep = 92,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SdkMessageProcessingStepImage = 93,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ServiceEndpoint = 95,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RoutingRule = 150,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RoutingRuleItem = 151,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SLA = 152,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SLAItem = 153,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ConvertRule = 154,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ConvertRuleItem = 155,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        HierarchyRule = 65,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        MobileOfflineProfile = 161,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        MobileOfflineProfileItem = 162,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SimilarityRule = 165,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        CustomControl = 66,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        CustomControlDefaultConfig = 68,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        CustomControlResource = 69,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ChannelAccessProfileEntityAccessLevel = 171,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ChannelAccessProfile = 172,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        DependencyFeature = 160,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ImportMap = 166,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SDKMessage = 201,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SDKMessageFilter = 202,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SdkMessagePair = 203,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SdkMessageRequest = 204,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SdkMessageRequestField = 205,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SdkMessageResponse = 206,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        SdkMessageResponseField = 207,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        WebWizard = 210,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ImportEntityMapping = 208,
    }
}