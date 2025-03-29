using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BlueDeep.Server.Services;

public class WebStatService
{
    private readonly MessageBrokerService _messageBrokerService;
    private readonly TopicService _topicService;

    public WebStatService(MessageBrokerService messageBrokerService, TopicService topicService)
    {
        _messageBrokerService = messageBrokerService;
        _topicService = topicService;
    }

    public string GetWebStatistics()
    {
        var topicsInfo = _messageBrokerService.GetTopicsInfo();
        
        foreach (var topicInfo in topicsInfo)
        {
            if (TopicService.TryGetSubscribers(topicInfo.Name, out var subscribers) && subscribers is not null)
                topicInfo.SetSubscribers(subscribers);
            else 
                topicInfo.SetSubscribers(null);
        }

        var retStr = new StringBuilder();
        retStr.AppendLine($"<div><h1>BlueDeep broker server statistics:</h1></div>");
        retStr.AppendLine($@"<table style=""
                                border: solid 1px grey;
                                padding: 10px;
                                width: 100%;
                                max-width: 42em;
                            ""><thead>
                                <tr style=""
                                    background: #e5e5e5;
                                "">
                                <th>TopicName</th>
                                <th>High</th>
                                <th>Low</th>
                                <th>Total</th>
                                <th>Subscribers</th>
                                </tr>");
        retStr.AppendLine($"</thead><tbody>");
        if (!topicsInfo.Any())
        {
            retStr.AppendLine("</tr>");
            retStr.AppendLine($"<td style=\"text-align:center;\">-</td>");
            retStr.AppendLine($"<td style=\"text-align:center;\">-</td>");
            retStr.AppendLine($"<td style=\"text-align:center;\">-</td>");
            retStr.AppendLine($"<td style=\"text-align:center;\">-</td>");
            retStr.AppendLine($"<td style=\"text-align:center;\">-</td>");
            retStr.AppendLine("</tr>");
        }
        else
        {
            foreach (var topicInfo in topicsInfo)
            {
                retStr.AppendLine("</tr>");
                retStr.AppendLine($"<td style=\"text-align:center;\"> {topicInfo.Name}</td>");
                retStr.AppendLine($"<td style=\"text-align:center;\">{topicInfo.PriorityHighCount}</td>");
                retStr.AppendLine($"<td style=\"text-align:center;\">{topicInfo.PriorityLowCount}</td>");
                retStr.AppendLine($"<td style=\"text-align:center;\">{topicInfo.TotalCount}</td>");
                retStr.AppendLine($"<td style=\"text-align:center;\">{topicInfo.GetSubscribers()?.Count ?? 0}</td>");
                retStr.AppendLine("</tr style=\"text-align:center;\">");
            }
        }

        retStr.AppendLine("</tbody></table>");
        return retStr.ToString();
    }
}