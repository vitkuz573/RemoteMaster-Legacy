// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostConfigurationGenerator
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private readonly HostConfiguration _model = new();
    public string? _selectedOrganization;
    public string? _selectedOrganizationalUnit;
    public List<Organization> _organizations = [];
    public List<OrganizationalUnit> _organizationalUnits = [];
    private readonly Dictionary<string, string> _countries = [];

    protected async override Task OnInitializedAsync()
    {
        var hostInformation = HostInformationService.GetHostInformation();

        _model.Server = hostInformation.Name;
        _model.Subject = new();

        await LoadUserOrganizationsAsync();

        _countries.Add("Afghanistan", "AF");
        _countries.Add("Albania", "AL");
        _countries.Add("Algeria", "DZ");
        _countries.Add("American Samoa", "AS");
        _countries.Add("Andorra", "AD");
        _countries.Add("Angola", "AO");
        _countries.Add("Anguilla", "AI");
        _countries.Add("Antarctica", "AQ");
        _countries.Add("Antigua and Barbuda", "AG");
        _countries.Add("Argentina", "AR");
        _countries.Add("Armenia", "AM");
        _countries.Add("Aruba", "AW");
        _countries.Add("Australia", "AU");
        _countries.Add("Austria", "AT");
        _countries.Add("Azerbaijan", "AZ");
        _countries.Add("Bahamas", "BS");
        _countries.Add("Bahrain", "BH");
        _countries.Add("Bangladesh", "BD");
        _countries.Add("Barbados", "BB");
        _countries.Add("Belarus", "BY");
        _countries.Add("Belgium", "BE");
        _countries.Add("Belize", "BZ");
        _countries.Add("Benin", "BJ");
        _countries.Add("Bermuda", "BM");
        _countries.Add("Bhutan", "BT");
        _countries.Add("Bolivia, Plurinational State of", "BO");
        _countries.Add("Bonaire, Sint Eustatius and Saba", "BQ");
        _countries.Add("Bosnia and Herzegovina", "BA");
        _countries.Add("Botswana", "BW");
        _countries.Add("Bouvet Island", "BV");
        _countries.Add("Brazil", "BR");
        _countries.Add("British Indian Ocean Territory", "IO");
        _countries.Add("Brunei Darussalam", "BN");
        _countries.Add("Bulgaria", "BG");
        _countries.Add("Burkina Faso", "BF");
        _countries.Add("Burundi", "BI");
        _countries.Add("Cabo Verde", "CV");
        _countries.Add("Cambodia", "KH");
        _countries.Add("Cameroon", "CM");
        _countries.Add("Canada", "CA");
        _countries.Add("Cayman Islands", "KY");
        _countries.Add("Central African Republic", "CF");
        _countries.Add("Chad", "TD");
        _countries.Add("Chile", "CL");
        _countries.Add("China", "CN");
        _countries.Add("Christmas Island", "CX");
        _countries.Add("Cocos (Keeling) Islands", "CC");
        _countries.Add("Colombia", "CO");
        _countries.Add("Comoros", "KM");
        _countries.Add("Congo", "CG");
        _countries.Add("Congo, The Democratic Republic of the", "CD");
        _countries.Add("Cook Islands", "CK");
        _countries.Add("Costa Rica", "CR");
        _countries.Add("Croatia", "HR");
        _countries.Add("Cuba", "CU");
        _countries.Add("Curaçao", "CW");
        _countries.Add("Cyprus", "CY");
        _countries.Add("Czechia", "CZ");
        _countries.Add("Côte d'Ivoire", "CI");
        _countries.Add("Denmark", "DK");
        _countries.Add("Djibouti", "DJ");
        _countries.Add("Dominica", "DM");
        _countries.Add("Dominican Republic", "DO");
        _countries.Add("Ecuador", "EC");
        _countries.Add("Egypt", "EG");
        _countries.Add("El Salvador", "SV");
        _countries.Add("Equatorial Guinea", "GQ");
        _countries.Add("Eritrea", "ER");
        _countries.Add("Estonia", "EE");
        _countries.Add("Eswatini", "SZ");
        _countries.Add("Ethiopia", "ET");
        _countries.Add("Falkland Islands (Malvinas)", "FK");
        _countries.Add("Faroe Islands", "FO");
        _countries.Add("Fiji", "FJ");
        _countries.Add("Finland", "FI");
        _countries.Add("France", "FR");
        _countries.Add("French Guiana", "GF");
        _countries.Add("French Polynesia", "PF");
        _countries.Add("French Southern Territories", "TF");
        _countries.Add("Gabon", "GA");
        _countries.Add("Gambia", "GM");
        _countries.Add("Georgia", "GE");
        _countries.Add("Germany", "DE");
        _countries.Add("Ghana", "GH");
        _countries.Add("Gibraltar", "GI");
        _countries.Add("Greece", "GR");
        _countries.Add("Greenland", "GL");
        _countries.Add("Grenada", "GD");
        _countries.Add("Guadeloupe", "GP");
        _countries.Add("Guam", "GU");
        _countries.Add("Guatemala", "GT");
        _countries.Add("Guernsey", "GG");
        _countries.Add("Guinea", "GN");
        _countries.Add("Guinea-Bissau", "GW");
        _countries.Add("Guyana", "GY");
        _countries.Add("Haiti", "HT");
        _countries.Add("Heard Island and McDonald Islands", "HM");
        _countries.Add("Holy See (Vatican City State)", "VA");
        _countries.Add("Honduras", "HN");
        _countries.Add("Hong Kong", "HK");
        _countries.Add("Hungary", "HU");
        _countries.Add("Iceland", "IS");
        _countries.Add("India", "IN");
        _countries.Add("Indonesia", "ID");
        _countries.Add("Iran, Islamic Republic of", "IR");
        _countries.Add("Iraq", "IQ");
        _countries.Add("Ireland", "IE");
        _countries.Add("Isle of Man", "IM");
        _countries.Add("Israel", "IL");
        _countries.Add("Italy", "IT");
        _countries.Add("Jamaica", "JM");
        _countries.Add("Japan", "JP");
        _countries.Add("Jersey", "JE");
        _countries.Add("Jordan", "JO");
        _countries.Add("Kazakhstan", "KZ");
        _countries.Add("Kenya", "KE");
        _countries.Add("Kiribati", "KI");
        _countries.Add("Korea, Democratic People's Republic of", "KP");
        _countries.Add("Korea, Republic of", "KR");
        _countries.Add("Kuwait", "KW");
        _countries.Add("Kyrgyzstan", "KG");
        _countries.Add("Lao People's Democratic Republic", "LA");
        _countries.Add("Latvia", "LV");
        _countries.Add("Lebanon", "LB");
        _countries.Add("Lesotho", "LS");
        _countries.Add("Liberia", "LR");
        _countries.Add("Libya", "LY");
        _countries.Add("Liechtenstein", "LI");
        _countries.Add("Lithuania", "LT");
        _countries.Add("Luxembourg", "LU");
        _countries.Add("Macao", "MO");
        _countries.Add("Madagascar", "MG");
        _countries.Add("Malawi", "MW");
        _countries.Add("Malaysia", "MY");
        _countries.Add("Maldives", "MV");
        _countries.Add("Mali", "ML");
        _countries.Add("Malta", "MT");
        _countries.Add("Marshall Islands", "MH");
        _countries.Add("Martinique", "MQ");
        _countries.Add("Mauritania", "MR");
        _countries.Add("Mauritius", "MU");
        _countries.Add("Mayotte", "YT");
        _countries.Add("Mexico", "MX");
        _countries.Add("Micronesia, Federated States of", "FM");
        _countries.Add("Moldova, Republic of", "MD");
        _countries.Add("Monaco", "MC");
        _countries.Add("Mongolia", "MN");
        _countries.Add("Montenegro", "ME");
        _countries.Add("Montserrat", "MS");
        _countries.Add("Morocco", "MA");
        _countries.Add("Mozambique", "MZ");
        _countries.Add("Myanmar", "MM");
        _countries.Add("Namibia", "NA");
        _countries.Add("Nauru", "NR");
        _countries.Add("Nepal", "NP");
        _countries.Add("Netherlands", "NL");
        _countries.Add("New Caledonia", "NC");
        _countries.Add("New Zealand", "NZ");
        _countries.Add("Nicaragua", "NI");
        _countries.Add("Niger", "NE");
        _countries.Add("Nigeria", "NG");
        _countries.Add("Niue", "NU");
        _countries.Add("Norfolk Island", "NF");
        _countries.Add("North Macedonia", "MK");
        _countries.Add("Northern Mariana Islands", "MP");
        _countries.Add("Norway", "NO");
        _countries.Add("Oman", "OM");
        _countries.Add("Pakistan", "PK");
        _countries.Add("Palau", "PW");
        _countries.Add("Palestine, State of", "PS");
        _countries.Add("Panama", "PA");
        _countries.Add("Papua New Guinea", "PG");
        _countries.Add("Paraguay", "PY");
        _countries.Add("Peru", "PE");
        _countries.Add("Philippines", "PH");
        _countries.Add("Pitcairn", "PN");
        _countries.Add("Poland", "PL");
        _countries.Add("Portugal", "PT");
        _countries.Add("Puerto Rico", "PR");
        _countries.Add("Qatar", "QA");
        _countries.Add("Romania", "RO");
        _countries.Add("Russian Federation", "RU");
        _countries.Add("Rwanda", "RW");
        _countries.Add("Réunion", "RE");
        _countries.Add("Saint Barthélemy", "BL");
        _countries.Add("Saint Helena, Ascension and Tristan da Cunha", "SH");
        _countries.Add("Saint Kitts and Nevis", "KN");
        _countries.Add("Saint Lucia", "LC");
        _countries.Add("Saint Martin (French part)", "MF");
        _countries.Add("Saint Pierre and Miquelon", "PM");
        _countries.Add("Saint Vincent and the Grenadines", "VC");
        _countries.Add("Samoa", "WS");
        _countries.Add("San Marino", "SM");
        _countries.Add("Sao Tome and Principe", "ST");
        _countries.Add("Saudi Arabia", "SA");
        _countries.Add("Senegal", "SN");
        _countries.Add("Serbia", "RS");
        _countries.Add("Seychelles", "SC");
        _countries.Add("Sierra Leone", "SL");
        _countries.Add("Singapore", "SG");
        _countries.Add("Sint Maarten (Dutch part)", "SX");
        _countries.Add("Slovakia", "SK");
        _countries.Add("Slovenia", "SI");
        _countries.Add("Solomon Islands", "SB");
        _countries.Add("Somalia", "SO");
        _countries.Add("South Africa", "ZA");
        _countries.Add("South Georgia and the South Sandwich Islands", "GS");
        _countries.Add("South Sudan", "SS");
        _countries.Add("Spain", "ES");
        _countries.Add("Sri Lanka", "LK");
        _countries.Add("Sudan", "SD");
        _countries.Add("Suriname", "SR");
        _countries.Add("Svalbard and Jan Mayen", "SJ");
        _countries.Add("Sweden", "SE");
        _countries.Add("Switzerland", "CH");
        _countries.Add("Syrian Arab Republic", "SY");
        _countries.Add("Taiwan, Province of China", "TW");
        _countries.Add("Tajikistan", "TJ");
        _countries.Add("Tanzania, United Republic of", "TZ");
        _countries.Add("Thailand", "TH");
        _countries.Add("Timor-Leste", "TL");
        _countries.Add("Togo", "TG");
        _countries.Add("Tokelau", "TK");
        _countries.Add("Tonga", "TO");
        _countries.Add("Trinidad and Tobago", "TT");
        _countries.Add("Tunisia", "TN");
        _countries.Add("Turkey", "TR");
        _countries.Add("Turkmenistan", "TM");
        _countries.Add("Turks and Caicos Islands", "TC");
        _countries.Add("Tuvalu", "TV");
        _countries.Add("Uganda", "UG");
        _countries.Add("Ukraine", "UA");
        _countries.Add("United Arab Emirates", "AE");
        _countries.Add("United Kingdom", "GB");
        _countries.Add("United States", "US");
        _countries.Add("United States Minor Outlying Islands", "UM");
        _countries.Add("Uruguay", "UY");
        _countries.Add("Uzbekistan", "UZ");
        _countries.Add("Vanuatu", "VU");
        _countries.Add("Venezuela, Bolivarian Republic of", "VE");
        _countries.Add("Viet Nam", "VN");
        _countries.Add("Virgin Islands, British", "VG");
        _countries.Add("Virgin Islands, U.S.", "VI");
        _countries.Add("Wallis and Futuna", "WF");
        _countries.Add("Western Sahara", "EH");
        _countries.Add("Yemen", "YE");
        _countries.Add("Zambia", "ZM");
        _countries.Add("Zimbabwe", "ZW");
        _countries.Add("Åland Islands", "AX");
    }

    private async Task OnValidSubmit(EditContext context)
    {
        _model.Subject.Organization = _selectedOrganization;
        _model.Subject.OrganizationalUnit = [_selectedOrganizationalUnit];

        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

        var jsonContent = JsonSerializer.Serialize(_model, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await module.InvokeVoidAsync("downloadDataAsFile", jsonContent, "RemoteMaster.Host.json", "application/json");
    }

    public void DownloadHost()
    {
        NavigationManager.NavigateTo("api/HostConfiguration/download-host", true);
    }

    private async Task LoadUserOrganizationsAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity.IsAuthenticated)
        {
            var username = user.Identity.Name;
            var appUser = await UserManager.Users
                                           .Include(u => u.AccessibleOrganizations)
                                           .FirstOrDefaultAsync(u => u.UserName == username);

            if (appUser != null)
            {
                _organizations = [.. appUser.AccessibleOrganizations];
            }
        }
    }

    private async Task LoadOrganizationalUnitsAsync()
    {
        if (!string.IsNullOrEmpty(_selectedOrganization))
        {
            var organization = _organizations.FirstOrDefault(o => o.Name == _selectedOrganization);
            
            if (organization != null)
            {
                var authState = await AuthenticationStateTask;
                var user = authState.User;

                if (user.Identity.IsAuthenticated)
                {
                    var username = user.Identity.Name;
                    var appUser = await UserManager.Users
                                                   .Include(u => u.AccessibleOrganizationalUnits)
                                                   .FirstOrDefaultAsync(u => u.UserName == username);

                    if (appUser != null)
                    {
                        _organizationalUnits = appUser.AccessibleOrganizationalUnits
                                                      .Where(ou => ou.OrganizationId == organization.NodeId)
                                                      .ToList();
                    }
                }
            }
        }
    }

    private async Task OrganizationChanged(string value)
    {
        _selectedOrganization = value;
        await LoadOrganizationalUnitsAsync();

        var selectedOrg = _organizations.FirstOrDefault(org => org.Name == value);

        Log.Information($"OrganizationChanged called with value: {value}");

        if (selectedOrg != null)
        {
            Log.Information($"Selected Organization: {selectedOrg.Name}");

            _model.Subject.Locality = selectedOrg.Locality;
            _model.Subject.State = selectedOrg.State;
            _model.Subject.Country = selectedOrg.Country;

            StateHasChanged();
        }
        else
        {
            Log.Error("No organization selected");
        }
    }

    private void OrganizationalUnitChanged(string value)
    {
        _selectedOrganizationalUnit = value;

        StateHasChanged();
    }
}
