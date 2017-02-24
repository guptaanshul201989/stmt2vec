Stmt2vec training code with the paper "Stmt2vec: Convolving Statement Embeddings on Program Dependence Graph for Extreme Summarization of Source Code".

This code is based on the code presented on https://github.com/mast-group/convolutional-attention.

This code is written in python. To use it you will need:
*. Python 2.7
*. Latest version of NumPy, SciPy and Theano 

To train the stmt2vec model for extreme summarization:

> python stmt2vec_learner.py <project_path>

The optimal parameters of this model has been encoded in the source code.

The most important parameters include the dimension of the token embeddings, the dimensions of the hidden layers of GRU, the window size of the convolution layer, the learning rate, the dropout rate, the init_scale, etc. 

We have experimented on these parameters, for example, the dimension of the token embeddings ranging from 64 to 256, the first GRU hidden layer ranging from 16 to 64, the layer3_window_size ranging from 3 to 17, and the dropout_rate ranging from 0.3 - 0.7.
