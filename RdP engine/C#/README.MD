# RdP Engine   version 0.4

## API

### class **PetriNet**

public PetriNet()

public PetriNet(String filename)

public Boolean ExecCycle()

public Boolean IsTriggerable(Transition t)

public Boolean LoadFromFile(String filename)

##### _Insertion methods_:

public void InsertTransition(Transition transition)

public void InsertPlace(Place place)

public void InsertConnection(Connection conn)

##### _Get methods_:

public Place GetPlaceById(int id)

public Place GetPlaceByLabel(String label)

public Transition GetTransitionById(int id)

public Transition GetTransitionByLabel(String label)

public Connection GetConnection(int sourceId, int destinationId)

##### _Remove methods_:

public Boolean RemovePlaceById(int id)

public Boolean RemoveTransitionById(int id)

public Boolean RemoveConnection(int sourceId, int destinationId)


### class **Place**

#### Properties:
        int iD;
        String label;
        int tokens;

public Place()

public void AddTokens(int quantity)

public Boolean RemTokens(int quantity)

public Boolean IsEmpty()

### class **Transition**

#### Properties:
        int id;
        String label;
        bool enabled;
        bool _triggerable;
        int priority;

public Transition()

### class **Connection**

#### Properties:
        ArcTypes type;
        int sourceId;
        int destinationId;
        int multiplicity;
    
 public Connection()
 
 Obs.:
 public enum ArcTypes
 {
     Regular = 0,
     Inhibitor = 1,
     Reset = 2
 }
