// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using LEGOMaterials;

namespace LEGOModelImporter
{

    public class Tube : CommonPart
    {
        public List<int> connections = new List<int>();
        public ConnectionField field;

        public override bool IsVisible()
        {
            foreach (var connectionIndex in connections)
            {
                var connection = field.connections[connectionIndex];
                // FIXME Temporary fix to tube removal while we work on connections that are related/non-rejecting but not connected.
                // FIXME This fix allows for tubes to work without reimporting existing models. Should be removed at a later state.
                if (connection.IsRelevantForTube())
                {
                    var connectedTo = connection.field.GetConnection(connection);
                    if (connectedTo == null)
                    {
                        return true;
                    }

                    var notCovering = (connection.flags & Connection.flagsCoveringTube) == 0 || (connectedTo.flags & Connection.flagsCoveringTube) == 0;
                    notCovering |= MouldingColour.IsAnyTransparent(part.materialIDs) || MouldingColour.IsAnyTransparent(connectedTo.field.connectivity.part.materialIDs);

                    if (notCovering)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}


