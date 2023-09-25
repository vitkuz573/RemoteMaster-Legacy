// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Agent.Helpers.AdvFirewall;

public class FirewallRule
{
    public string Name { get; set; }

    public RuleDirection Direction { get; set; }

    public RuleAction Action { get; set; }

    public RuleProtocol Protocol { get; set; }

    public string LocalPort { get; set; }

    public List<RuleProfile> Profiles { get; } = new List<RuleProfile>();

    public string Program { get; set; }

    public string Service { get; set; }

    public FirewallRule(string name)
    {
        Name = name;
    }

    public void Apply()
    {
        FirewallManager.AddRule(this);
    }

    public void Delete()
    {
        FirewallManager.DeleteRule(Name, Direction);
    }

    public string ShowDetails()
    {
        return FirewallManager.ShowRuleDetails(Name);
    }
}
