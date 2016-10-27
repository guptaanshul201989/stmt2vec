import theano
import theano.tensor as tensor
import numpy
from collections import OrderedDict

def _p(pp, name):
    """
    Make prefix-appended name
    """
    return '%s_%s'%(pp, name)

def ortho_weight(ndim):
    """
    Orthogonal weight init, for recurrent layers
    """
    W = numpy.random.randn(ndim, ndim)
    u, s, v = numpy.linalg.svd(W)
    return u

def norm_weight(nin,nout=None, scale=0.1, ortho=True):
    """
    Uniform initalization from [-scale, scale]
    If matrix is square and ortho=True, use ortho instead
    """
    if nout == None:
        nout = nin
    if nout == nin and ortho:
        W = ortho_weight(nin)
    else:
        W = numpy.random.uniform(low=-scale, high=scale, size=(nin, nout))
    return W

def tanh(x):
    """
    Tanh activation function
    """
    return tensor.tanh(x)

def linear(x):
    """
    Linear activation function
    """
    return x
