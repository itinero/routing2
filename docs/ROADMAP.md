#### Profiles & data storage

We need to develop a way to efficiëntly store data on edges (inline or separate). The suggested approach now is to create 'datahandlers' that can read/write data from/to a byte array. These are grouped into a 'dataprofile' for a graph that contains the layout of the data together with the handlers.

