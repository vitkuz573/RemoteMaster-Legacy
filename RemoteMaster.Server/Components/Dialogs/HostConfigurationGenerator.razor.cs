// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostConfigurationGenerator
{
    private readonly HostConfiguration _model = new();
    private readonly List<string> _countryCodes = [];

    protected override void OnInitialized()
    {
        _model.Server = GetLocalIpAddress();
        _model.Subject = new();

        _countryCodes.Add("AF");
        _countryCodes.Add("AX");
        _countryCodes.Add("AL");
        _countryCodes.Add("DZ");
        _countryCodes.Add("AS");
        _countryCodes.Add("AD");
        _countryCodes.Add("AO");
        _countryCodes.Add("AI");
        _countryCodes.Add("AQ");
        _countryCodes.Add("AG");
        _countryCodes.Add("AR");
        _countryCodes.Add("AM");
        _countryCodes.Add("AW");
        _countryCodes.Add("AU");
        _countryCodes.Add("AT");
        _countryCodes.Add("AZ");
        _countryCodes.Add("BS");
        _countryCodes.Add("BH");
        _countryCodes.Add("BD");
        _countryCodes.Add("BB");
        _countryCodes.Add("BY");
        _countryCodes.Add("BE");
        _countryCodes.Add("BZ");
        _countryCodes.Add("BJ");
        _countryCodes.Add("BM");
        _countryCodes.Add("BT");
        _countryCodes.Add("BO");
        _countryCodes.Add("BQ");
        _countryCodes.Add("BA");
        _countryCodes.Add("BW");
        _countryCodes.Add("BV");
        _countryCodes.Add("BR");
        _countryCodes.Add("IO");
        _countryCodes.Add("BN");
        _countryCodes.Add("BG");
        _countryCodes.Add("BF");
        _countryCodes.Add("BI");
        _countryCodes.Add("CV");
        _countryCodes.Add("KH");
        _countryCodes.Add("CM");
        _countryCodes.Add("CA");
        _countryCodes.Add("KY");
        _countryCodes.Add("CF");
        _countryCodes.Add("TD");
        _countryCodes.Add("CL");
        _countryCodes.Add("CN");
        _countryCodes.Add("CX");
        _countryCodes.Add("CC");
        _countryCodes.Add("CO");
        _countryCodes.Add("KM");
        _countryCodes.Add("CG");
        _countryCodes.Add("CD");
        _countryCodes.Add("CK");
        _countryCodes.Add("CR");
        _countryCodes.Add("CI");
        _countryCodes.Add("HR");
        _countryCodes.Add("CU");
        _countryCodes.Add("CW");
        _countryCodes.Add("CY");
        _countryCodes.Add("CZ");
        _countryCodes.Add("DK");
        _countryCodes.Add("DJ");
        _countryCodes.Add("DM");
        _countryCodes.Add("DO");
        _countryCodes.Add("EC");
        _countryCodes.Add("EG");
        _countryCodes.Add("SV");
        _countryCodes.Add("GQ");
        _countryCodes.Add("ER");
        _countryCodes.Add("EE");
        _countryCodes.Add("SZ");
        _countryCodes.Add("ET");
        _countryCodes.Add("FK");
        _countryCodes.Add("FO");
        _countryCodes.Add("FJ");
        _countryCodes.Add("FI");
        _countryCodes.Add("FR");
        _countryCodes.Add("GF");
        _countryCodes.Add("PF");
        _countryCodes.Add("TF");
        _countryCodes.Add("GA");
        _countryCodes.Add("GM");
        _countryCodes.Add("GE");
        _countryCodes.Add("DE");
        _countryCodes.Add("GH");
        _countryCodes.Add("GI");
        _countryCodes.Add("GR");
        _countryCodes.Add("GL");
        _countryCodes.Add("GD");
        _countryCodes.Add("GP");
        _countryCodes.Add("GU");
        _countryCodes.Add("GT");
        _countryCodes.Add("GG");
        _countryCodes.Add("GN");
        _countryCodes.Add("GW");
        _countryCodes.Add("GY");
        _countryCodes.Add("HT");
        _countryCodes.Add("HM");
        _countryCodes.Add("VA");
        _countryCodes.Add("HN");
        _countryCodes.Add("HK");
        _countryCodes.Add("HU");
        _countryCodes.Add("IS");
        _countryCodes.Add("IN");
        _countryCodes.Add("ID");
        _countryCodes.Add("IR");
        _countryCodes.Add("IQ");
        _countryCodes.Add("IE");
        _countryCodes.Add("IM");
        _countryCodes.Add("IL");
        _countryCodes.Add("IT");
        _countryCodes.Add("JM");
        _countryCodes.Add("JP");
        _countryCodes.Add("JE");
        _countryCodes.Add("JO");
        _countryCodes.Add("KZ");
        _countryCodes.Add("KE");
        _countryCodes.Add("KI");
        _countryCodes.Add("KP");
        _countryCodes.Add("KR");
        _countryCodes.Add("KW");
        _countryCodes.Add("KG");
        _countryCodes.Add("LA");
        _countryCodes.Add("LV");
        _countryCodes.Add("LB");
        _countryCodes.Add("LS");
        _countryCodes.Add("LR");
        _countryCodes.Add("LY");
        _countryCodes.Add("LI");
        _countryCodes.Add("LT");
        _countryCodes.Add("LU");
        _countryCodes.Add("MO");
        _countryCodes.Add("MG");
        _countryCodes.Add("MW");
        _countryCodes.Add("MY");
        _countryCodes.Add("MV");
        _countryCodes.Add("ML");
        _countryCodes.Add("MT");
        _countryCodes.Add("MH");
        _countryCodes.Add("MQ");
        _countryCodes.Add("MR");
        _countryCodes.Add("MU");
        _countryCodes.Add("YT");
        _countryCodes.Add("MX");
        _countryCodes.Add("FM");
        _countryCodes.Add("MD");
        _countryCodes.Add("MC");
        _countryCodes.Add("MN");
        _countryCodes.Add("ME");
        _countryCodes.Add("MS");
        _countryCodes.Add("MA");
        _countryCodes.Add("MZ");
        _countryCodes.Add("MM");
        _countryCodes.Add("NA");
        _countryCodes.Add("NR");
        _countryCodes.Add("NP");
        _countryCodes.Add("NL");
        _countryCodes.Add("NC");
        _countryCodes.Add("NZ");
        _countryCodes.Add("NI");
        _countryCodes.Add("NE");
        _countryCodes.Add("NG");
        _countryCodes.Add("NU");
        _countryCodes.Add("NF");
        _countryCodes.Add("MK");
        _countryCodes.Add("MP");
        _countryCodes.Add("NO");
        _countryCodes.Add("OM");
        _countryCodes.Add("PK");
        _countryCodes.Add("PW");
        _countryCodes.Add("PS");
        _countryCodes.Add("PA");
        _countryCodes.Add("PG");
        _countryCodes.Add("PY");
        _countryCodes.Add("PE");
        _countryCodes.Add("PH");
        _countryCodes.Add("PN");
        _countryCodes.Add("PL");
        _countryCodes.Add("PT");
        _countryCodes.Add("PR");
        _countryCodes.Add("QA");
        _countryCodes.Add("RE");
        _countryCodes.Add("RO");
        _countryCodes.Add("RU");
        _countryCodes.Add("RW");
        _countryCodes.Add("BL");
        _countryCodes.Add("SH");
        _countryCodes.Add("KN");
        _countryCodes.Add("LC");
        _countryCodes.Add("MF");
        _countryCodes.Add("PM");
        _countryCodes.Add("VC");
        _countryCodes.Add("WS");
        _countryCodes.Add("SM");
        _countryCodes.Add("ST");
        _countryCodes.Add("SA");
        _countryCodes.Add("SN");
        _countryCodes.Add("RS");
        _countryCodes.Add("SC");
        _countryCodes.Add("SL");
        _countryCodes.Add("SG");
        _countryCodes.Add("SX");
        _countryCodes.Add("SK");
        _countryCodes.Add("SI");
        _countryCodes.Add("SB");
        _countryCodes.Add("SO");
        _countryCodes.Add("ZA");
        _countryCodes.Add("GS");
        _countryCodes.Add("SS");
        _countryCodes.Add("ES");
        _countryCodes.Add("LK");
        _countryCodes.Add("SD");
        _countryCodes.Add("SR");
        _countryCodes.Add("SJ");
        _countryCodes.Add("SE");
        _countryCodes.Add("CH");
        _countryCodes.Add("SY");
        _countryCodes.Add("TW");
        _countryCodes.Add("TJ");
        _countryCodes.Add("TZ");
        _countryCodes.Add("TH");
        _countryCodes.Add("TL");
        _countryCodes.Add("TG");
        _countryCodes.Add("TK");
        _countryCodes.Add("TO");
        _countryCodes.Add("TT");
        _countryCodes.Add("TN");
        _countryCodes.Add("TR");
        _countryCodes.Add("TM");
        _countryCodes.Add("TC");
        _countryCodes.Add("TV");
        _countryCodes.Add("UG");
        _countryCodes.Add("UA");
        _countryCodes.Add("AE");
        _countryCodes.Add("GB");
        _countryCodes.Add("UM");
        _countryCodes.Add("US");
        _countryCodes.Add("UY");
        _countryCodes.Add("UZ");
        _countryCodes.Add("VU");
        _countryCodes.Add("VE");
        _countryCodes.Add("VN");
        _countryCodes.Add("VG");
        _countryCodes.Add("VI");
        _countryCodes.Add("WF");
        _countryCodes.Add("EH");
        _countryCodes.Add("YE");
    }

    private async Task OnValidSubmit(EditContext context)
    {
        await JsRuntime.InvokeVoidAsync("generateAndDownloadFile", _model);

        StateHasChanged();
    }

    public void DownloadHost()
    {
        NavigationManager.NavigateTo("api/HostConfiguration/download-host", true);
    }

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
