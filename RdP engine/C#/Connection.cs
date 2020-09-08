using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdPengine
{
public enum ArcTypes
{
    Regular = 0,
    Inhibitor = 1,
    Reset = 2
}

public class Connection
{
    public ArcTypes Type
    {
        get => _type;
        set => _type = value;
    }

    public int SourceId
    {
        get => _sourceId;
        set => _sourceId = value;
    }

    public int DestinationId
    {
        get => _destinationId;
        set => _destinationId = value;
    }

    public int Multiplicity
    {
        get => _multiplicity;
        set => _multiplicity = value;
    }

    private ArcTypes _type;
    private int _sourceId;
    private int _destinationId;
    private int _multiplicity;

    public Connection()
    {

    }
}
}

